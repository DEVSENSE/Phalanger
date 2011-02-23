[expect php]
[file]
<?php
  require('Phalanger.inc');
	__var_dump(strripos("test test string", "test"));
	__var_dump(strripos("test string sTring", "string"));
	__var_dump(strripos("test strip string strand", "str"));
	__var_dump(strripos("I am what I am and that's all what I am", "am", -3));
	__var_dump(strripos("test string", "g"));
	__var_dump(strripos("te".chr(0)."st", chr(0)));
	__var_dump(strripos("tEst", "test"));
	__var_dump(strripos("teSt", "test"));
	__var_dump(@strripos("foo", "f", 1));
	__var_dump(@strripos("", ""));
	__var_dump(@strripos("a", ""));
	__var_dump(@strripos("", "a"));
	__var_dump(@strripos("\\\\a", "\\a"));
?>