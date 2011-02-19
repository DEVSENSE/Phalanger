[expect php]
[file]

<?php
include 'fs.inc';
// get contents of a file into a string
echo "[TEXT]";
$filename = "test.txt";
$handle = fopen($filename, "rt");
$contents = fread($handle, filesize($filename));
fclose($handle);
echo strlen($contents);
?>  
