[expect php]
[file]
<?php
  require('Phalanger.inc');

	__var_dump(strstr("test string", "test"));
	__var_dump(strstr("test string", "string"));
	__var_dump(strstr("test string", "strin"));
	__var_dump(strstr("test string", "t s"));
	__var_dump(strstr("test string", "g"));
	__var_dump(md5(strstr("te".chr(0)."st", chr(0))));
	__var_dump(strstr("tEst", "test"));
	__var_dump(strstr("teSt", "test"));
	__var_dump(@strstr("", ""));
	__var_dump(@strstr("a", ""));
	__var_dump(@strstr("", "a"));
	__var_dump(md5(@strstr("\\\\a\\", "\\a")));
?>
