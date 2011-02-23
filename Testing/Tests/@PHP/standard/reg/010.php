[expect php]
[file]
<?php $a="abc122222222223";
  echo ereg_replace("1(2*)3","\\1def\\1",$a)?>
