[expect php]
[file]
<?php
  require('Phalanger.inc');
	__var_dump(strrpos("test test string", "test"));
	__var_dump(strrpos("test string sTring", "string"));
	__var_dump(strrpos("test strip string strand", "str"));
	__var_dump(strrpos("I am what I am and that's all what I am", "am", -3));
	__var_dump(strrpos("test string", "g"));
	__var_dump(strrpos("te".chr(0)."st", chr(0)));
	__var_dump(strrpos("tEst", "test"));
	__var_dump(strrpos("teSt", "test"));
	__var_dump(@strrpos("foo", "f", 1));
	__var_dump(@strrpos("", ""));
	__var_dump(@strrpos("a", ""));
	__var_dump(@strrpos("", "a"));
	__var_dump(@strrpos("\\\\a", "\\a"));
?>
