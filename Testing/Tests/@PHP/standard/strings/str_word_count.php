[expect php]
[file]
<?php
  require('Phalanger.inc');
error_reporting(0);
$str = "Hello friend, you're\r\nlooking          good today!";
$b =& $str;       
__var_dump(str_word_count($str, 1));
__var_dump(str_word_count($str, 2));
__var_dump(str_word_count($str));
__var_dump(str_word_count($str, 3)); 
__var_dump(str_word_count($str, 123));
__var_dump(str_word_count($str, -1));
__var_dump(str_word_count($str, 99999999999999999));
// invalid input: __var_dump(str_word_count($str, array()));
// invalid input: __var_dump(str_word_count($str, $b));
__var_dump($str);

$str2 = "F0o B4r 1s bar foo";
__var_dump(str_word_count($str2, NULL, "04"));
__var_dump(str_word_count($str2, NULL, "01"));
__var_dump(str_word_count($str2, NULL, "014"));
// invalid input: __var_dump(str_word_count($str2, NULL, array()));
// invalid input: __var_dump(str_word_count($str2, NULL, new stdClass));
__var_dump(str_word_count($str2, NULL, ""));
__var_dump(str_word_count($str2, 1, "04"));
__var_dump(str_word_count($str2, 1, "01"));
__var_dump(str_word_count($str2, 1, "014"));
// invalid input: __var_dump(str_word_count($str2, 1, array()));
// invalid input: __var_dump(str_word_count($str2, 1, new stdClass));
__var_dump(str_word_count($str2, 1, ""));
__var_dump(str_word_count($str2, 2, "04"));
__var_dump(str_word_count($str2, 2, "01"));
__var_dump(str_word_count($str2, 2, "014"));
// invalid input: __var_dump(str_word_count($str2, 2, array()));
// invalid input: __var_dump(str_word_count($str2, 2, new stdClass));
__var_dump(str_word_count($str2, 2, ""));
__var_dump(str_word_count("foo'0 bar-0var", 2, "0"));
__var_dump(str_word_count("'foo'", 2));
__var_dump(str_word_count("'foo'", 2, "'"));
__var_dump(str_word_count("-foo-", 2));
__var_dump(str_word_count("-foo-", 2, "-"));
?>