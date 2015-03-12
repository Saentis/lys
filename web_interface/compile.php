<?php
/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

// Check if we got everything we need
if (!isset($_POST['source']))
{
	header('HTTP/1.0 400 Bad Request');
	header('Content-Type: text/plain');
	die('Missing parameters.');
}

// Open process
$pathToCompiler = realpath('./compiler/Lys.exe');
$descriptorspec = array(
	0 => array('pipe', 'r'), // stdin
	1 => array('pipe', 'w'), // stdout
	2 => array('pipe', 'w'), // stderr
);
$pipes;
$process = proc_open($pathToCompiler, $descriptorspec, $pipes);
if (is_resource($process))
{
	// Write into stdin
	fwrite($pipes[0], $_POST['source']);
	fclose($pipes[0]);

	// Read from stdout and stderr
	$compiled = stream_get_contents($pipes[1]);
	$errors = stream_get_contents($pipes[2]);
	fclose($pipes[1]);
	fclose($pipes[2]);

	// Close process and check if compilation was successful
	$status = proc_close($process);

	$result = array('status' => $status);
	if ($status != 0)
	{
		$result['str'] = $errors;
	}
	else
	{
		$tempFile = tempnam(sys_get_temp_dir(), 'lys');
		file_put_contents($tempFile, $compiled);

		// Create a unique token
		// For security reasons the filename is enclosed by '[]' - that way, no additional data can be pre-/appended.
		$token = '[' . basename($tempFile) . ']';
		$iv = openssl_random_pseudo_bytes(openssl_cipher_iv_length('aes128'));
		$key = "\xce\xa9\x0c\xbc\x2b\x79\x88\x72\x21\xe7\x7d\x78\x29\x46\x42\xd3";
		$token = openssl_encrypt($token, 'aes128', $key, OPENSSL_RAW_DATA, $iv);
		$result['token'] = str_replace(array('/', '+'), array('_', '-'), base64_encode($token));
		$result['iv'] = str_replace(array('/', '+'), array('_', '-'), base64_encode($iv));
	}
	$result = json_encode($result);
	header('Content-Type: application/json');
	header('Content-Length: ' . strlen($result));
	echo $result;
	exit;
}

?>