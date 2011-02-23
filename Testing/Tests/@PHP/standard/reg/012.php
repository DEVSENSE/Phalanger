[expect php]
[file]
<?php $a="abc123";
  echo ereg_replace("123",'def\1ghi',$a)?>
