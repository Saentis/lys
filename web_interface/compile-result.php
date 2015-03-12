<?php
/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

// Check if we got everything we need
if (!isset($_GET['token']) || !isset($_GET['iv']) || !isset($_GET['callback']))
{
	header('HTTP/1.0 400 Bad Request');
	header('Content-Type: text/plain');
	die('Missing parameters.');
}

// Verify parameters
if (!preg_match('#^[a-zA-Z0-9_-]+={0,2}$#', $_GET['token']) || !preg_match('#^[a-zA-Z0-9_-]+={0,2}$#', $_GET['iv']))
{
	header('HTTP/1.0 400 Bad Request');
	header('Content-Type: text/plain');
	die('Expected (urlsafe) base64-encoded token/iv parameters.');
}
if (!preg_match('#^([a-z_$][a-z0-9_$]*(\.|$))+(?<!\.)$#i', $_GET['callback']))
{
	header('HTTP/1.0 400 Bad Request');
	header('Content-Type: text/plain');
	die('Invalid javascript callback function.');
}

// Decode data
$token = base64_decode(str_replace(array('_', '-'), array('/', '+'), $_GET['token']));
$iv = base64_decode(str_replace(array('_', '-'), array('/', '+'), $_GET['iv']));

// Decrypt data
$key = "\xce\xa9\x0c\xbc\x2b\x79\x88\x72\x21\xe7\x7d\x78\x29\x46\x42\xd3";
$tempFileName = openssl_decrypt($token, 'aes128', $key, OPENSSL_RAW_DATA, $iv);
// For security reasons the filename is enclosed by '[]' - that way, no additional data can be pre-/appended.
if (!$tempFileName || !preg_match('#^\[[^\\/]+\]$#', $tempFileName))
{
	header('HTTP/1.0 400 Bad Request');
	header('Content-Type: text/plain');
	die('The application token could not be decrypted.');
}
$tempFileName = substr($tempFileName, 1, -1); // remove '[' and ']' delimiters again

// Check if file exists
// Note that $tempFileName does not contain directory separators, so there is no possibility
// for this path to point somewhere outside of /tmp/.
// Moreover, sys_get_temp_dir() adds/does not add a a trailing slash inconsistently across OS's,
// so we use realpath() to avoid this problem.
$tempFile = realpath(sys_get_temp_dir()) . DIRECTORY_SEPARATOR . $tempFileName;
if (!is_file($tempFile))
{
	header('HTTP/1.0 404 Not Found');
	header('Content-Type: text/plain');
	die('The requested application could not be found. Note that it is deleted automatically once it is retrieved the first time.'.$tempFile);
}

// Read content and delete the file
$content = file_get_contents($tempFile);
unlink($tempFile);

// Compilation result is the app object itself (enclosed by '{}')
$content = $_GET['callback'] . '((function($vm) { return ' . $content . '; })(vm));';

header('Content-Type: text/javascript');
header('Content-Length: ' . strlen($content));
echo $content;
exit;

?>