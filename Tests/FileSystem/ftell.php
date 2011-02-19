[expect php]
[file]

<xmp>
<?php
include 'fs.inc';

// opens a file and read some data
$fp = fopen("test.txt", "rt");
echo ($data = fgets($fp));
echo ":".ftell($fp);

echo ($data = fgets($fp));
echo ":".ftell($fp);

echo ($data = fgets($fp));
echo ":".ftell($fp);

fclose($fp);

echo "\n\n";

// opens a file and read some data
$fp = fopen("test.txt", "rb");
echo ($data = fgets($fp));
echo ":".ftell($fp);

echo ($data = fgets($fp));
echo ":".ftell($fp);

echo ($data = fgets($fp));
echo ":".ftell($fp);

fclose($fp);

?> 
</xmp>