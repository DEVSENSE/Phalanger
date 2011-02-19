[expect php]
[file]

TCP

<?php
$fp = fsockopen("www.google.com", 80, $errno, $errstr, 30);
if (!$fp) {
    echo "$errstr ($errno)<br />\n";
} else {
    $out = "GET / HTTP/1.1\r\n";
    $out .= "Host: www.example.com\r\n";
    $out .= "Connection: Close\r\n\r\n";

$len = 0;

    fwrite($fp, $out);
    while (!feof($fp)) {
        $len += strlen(fgets($fp, 128));
    }
    fclose($fp);

echo "READ: $len\n";
}
?>

UDP

<?php

/* REM
$fp = fsockopen("udp://127.0.0.1", 13, $errno, $errstr);
if (!$fp) {
    echo "ERROR: $errno - $errstr<br />\n";
} else {
    fwrite($fp, "\n");
    echo fread($fp, 26);
    fclose($fp);
}

/**/

?> 