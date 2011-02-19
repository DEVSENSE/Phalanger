[expect php]
[file]
<?php $a="This is a nice and simple string";
  if (ereg(".*nice and simple.*",$a)) {
    echo "ok\n";
  }
  if (!ereg(".*doesn't exist.*",$a)) {
    echo "ok\n";
  }
?>