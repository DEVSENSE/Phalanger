[expect php]
[file]

<?php

include 'fs.inc';


$fp = fopen('test.txt', 'rb');

// read some data
$data = fgets($fp, 4096);
echo ":" . ftell($fp);

// move back to the beginning of the file
// same as rewind($fp);
D(fseek($fp, 0));
echo ":" . ftell($fp);
echo fgets($fp);

D(fseek($fp, 123));
echo ":" . ftell($fp);
echo fgets($fp);

D(fseek($fp, 1234567));
echo ":" . ftell($fp);
echo fgets($fp);

fclose($fp);



$fp = fopen('test.txt', 'rt');

// read some data
$data = fgets($fp, 4096);
echo ":" . ftell($fp);

// move back to the beginning of the file
// same as rewind($fp);
@fseek($fp, 0);
echo ":" . ftell($fp);

@fseek($fp, 123);
echo ":" . ftell($fp);

@fseek($fp, 1234567);
echo ":" . ftell($fp);

fclose($fp);

?> 