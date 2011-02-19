[expect php]
[file]

<?php
include 'fs.inc';
// get contents of a file into a string
echo "[BIN]";
$filename = "test.dat";
$handle = fopen($filename, "rb");
$contents = fread($handle, filesize($filename));
fclose($handle);
echo strlen($contents);
?>  
