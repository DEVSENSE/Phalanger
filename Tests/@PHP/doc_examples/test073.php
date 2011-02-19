[expect php]

[file]
<?php
$i = 'W';
for($n=0; $n<6; $n++)
  echo ++$i . "\n";

/*
  Produces the output similar to the following:

X
Y
Z
AA
AB
AC

*/
?>
