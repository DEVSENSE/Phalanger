[expect php]
[file]

<?php

include "fs.inc";

$handle = fopen("test.txt", "rt");
if ($handle)
{
  $i = 0;
  $contents = '';
  while (!feof($handle)) {

	// Note: Phalanger returns EOF one fgets sooner!

    $len = fgets($handle, 1024);
    $contents .= $len;
    if ($len) print "|" . ftell($handle) . "->" . strlen($contents);
    if ((++$i & 7) == 0) echo "\r\n";
  }
  fclose($handle);

  print "\r\n" . strlen($contents);
}

?>