[expect php]
[file]

<?php
include 'fs.inc';
$handle = fopen("test.dat", "rb");
if ($handle)
{
  $contents = '';
  while (!feof($handle)) {
    $contents .= fread($handle, 1024);
  }
  fclose($handle);
  echo strlen($contents);
}
?>  
