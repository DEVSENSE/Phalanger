[expect php]
[file]
<?php
include('Phalanger.inc');
function __(){
  $GLOBALS['a'] = "bug\n";
  array_splice($GLOBALS,0,count($GLOBALS));
  /* All global variables including $GLOBALS are removed */
  @__var_dump($GLOBALS['a']);
}
__();
echo "ok\n";
?>