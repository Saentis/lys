<?php
/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

$exampleApps = array
(
	array('name' => 'Sine animation 1', 'w' => 15, 'h' => 10, 'entry' => 'examples::sine1::main', 'code' => file_get_contents('apps/sine1.lys')),
	array('name' => 'Vector manipulation 1', 'w' => 10, 'h' => 10, 'entry' => 'examples::vectorManipulation1::main', 'code' => file_get_contents('apps/vectorManipulation1.lys')),
	array('name' => 'Circular animation 1', 'w' => 15, 'h' => 10, 'entry' => 'examples::circular1::main', 'code' => file_get_contents('apps/circular1.lys')),
	array('name' => 'Circular animation 2', 'w' => 15, 'h' => 10, 'entry' => 'examples::circular2::main', 'code' => file_get_contents('apps/circular2.lys')),
	array('name' => 'The Matrix', 'w' => 20, 'h' => 15, 'entry' => 'examples::matrix1::main', 'code' => file_get_contents('apps/matrix1.lys')),
);

?>
<!DOCTYPE html>
<html>
<head>
<title>LEDtable</title>
<meta http-equiv="Content-Type" content="text/html;charset=utf-8" />
<link rel="stylesheet" type="text/css" href="style/main.css" />
<link rel="stylesheet" type="text/css" href="//fonts.googleapis.com/css?family=Open+Sans+Condensed:300" />
<script type="text/javascript" src="//code.jquery.com/jquery-1.6.4.min.js"></script>
<script type="text/javascript" src="//cdnjs.cloudflare.com/ajax/libs/jquery.transit/0.9.9/jquery.transit.min.js"></script>
<script type="text/javascript" src="js/lysvm.js"></script>
<script type="text/javascript" src="js/layout.js"></script>
<script type="text/javascript">/*<![CDATA[*/
var vm = new LysVM();
var layout = new Layout(vm, <?=json_encode($exampleApps)?>);
$(function() { layout.load(); });
/*]]>*/</script>
</head>
<body>

<!--
<h1>LEDtable virtual machine</h1>
-->

<p id="loading">Please wait while the layout is being initialized ...</p>

</body>
</html>