[expect php]
[file]
<?php
  require('Phalanger.inc');

/* Do not change this test it is a README.TESTING example. */
$trans = array("hello"=>"hi", "hi"=>"hello", "a"=>"A", "world"=>"planet");
__var_dump(strtr("# hi all, I said hello world! #", $trans));
?>
