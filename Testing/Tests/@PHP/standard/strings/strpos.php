[expect php]
[file]
<?php
  require('Phalanger.inc');
	__var_dump(strpos("test string", "test"));
	__var_dump(strpos("test string", "string"));
	__var_dump(strpos("test string", "strin"));
	__var_dump(strpos("test string", "t s"));
	__var_dump(strpos("test string", "g"));
	__var_dump(strpos("te".chr(0)."st", chr(0)));
	__var_dump(strpos("tEst", "test"));
	__var_dump(strpos("teSt", "test"));
	__var_dump(@strpos("", ""));
	__var_dump(@strpos("a", ""));
	__var_dump(@strpos("", "a"));
	__var_dump(@strpos("\\\\a", "\\a"));
?>