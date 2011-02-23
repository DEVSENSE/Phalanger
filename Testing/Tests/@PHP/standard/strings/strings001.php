[expect php]
[file]
<?php
  require('Phalanger.inc');
/* Do not change this test it is a README.TESTING example. */
$s = "alabala nica".chr(0)."turska panica";
__var_dump(strstr($s, "nic"));
__var_dump(strrchr($s," nic"));
?>