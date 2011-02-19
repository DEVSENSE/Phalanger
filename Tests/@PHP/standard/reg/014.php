[expect php]
[file]
<?php $a="a\\2bxc";
  echo ereg_replace("a(.*)b(.*)c","\\1",$a)?>
