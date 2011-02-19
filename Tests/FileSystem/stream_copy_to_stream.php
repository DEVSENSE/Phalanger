[expect php]
[file]
<?php
include('fs.inc');
define('f', 'readfile.php');


$fr = fopen(f, "rb");
$fw = fopen("php://stdout", "wb");
echo "Copied to STDOUT: ";
echo dump(stream_copy_to_stream($fr, $fw));
//fclose($fw); // Note: this REALLY closes the output of PHP!!!
fclose($fr); 

?> 
