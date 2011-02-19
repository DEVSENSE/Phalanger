[expect php]
[file]

<?php

// write out size in GB, bytes could change between running test and php..
$GB = 1024*1024*1024;

// $df contains the number of bytes available on "/"
echo ((int)(disk_free_space("/")/$GB)) . " GB\n";

// On Windows:
foreach (range('C','F') as $drive)
{
  echo "Drive $drive: ";
  $sp = disk_free_space("$drive:");
  if ($sp === false) echo "false\n";
  else echo ((int)($sp/$GB))." GB\n";
}

?> 