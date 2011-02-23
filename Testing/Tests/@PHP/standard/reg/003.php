[expect php]
[file]
<?php $a="\\'test";
  echo ereg_replace("\\\\'","'",$a)
?>