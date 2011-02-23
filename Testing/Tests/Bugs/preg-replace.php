[expect exact]
3_ 4_ 2_
[file]

<?php
  echo preg_replace("#\_+#","_","3___ 4____ 2__");
?>