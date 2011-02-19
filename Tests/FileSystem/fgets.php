[expect php]
[file]

<?php
include 'fs.inc';
$handle = fopen("test.txt", "rt");
if ($handle)
{
  while (!feof($handle)) {
    $buffer = fgets($handle, 4096);
    echo htmlspecialchars($buffer);
  }
  fclose($handle);
}
?> 