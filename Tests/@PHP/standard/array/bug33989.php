[expect php]
[file]
<?php
$a="a";
extract($GLOBALS, EXTR_REFS);
echo "ok\n";
?>
