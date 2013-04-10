--TEST--
Test preg_match() function : error conditions - bad regular expressions
--FILE--
<?php
/* 
 *  proto int preg_match(string pattern, string subject [, array subpatterns [, int flags [, int offset]]])
 * Function is implemented in ext/pcre/php_pcre.c
*/
error_reporting(E_ALL&~E_NOTICE);
/*
* Testing how preg_match reacts to being passed the wrong type of regex argument
*/
echo "*** Testing preg_match() : error conditions ***\n";
$regex_array = array('abcdef', //Regex without delimeter
'/[a-zA-Z]', //Regex without closing delimeter
'[a-zA-Z]/', //Regex without opening delimeter
'/[a-zA-Z]/F', array('[a-z]', //Array of Regexes
'[A-Z]', '[0-9]'), '/[a-zA-Z]/', //Regex string
);
$subject = 'this is a test';
foreach($regex_array as $regex_value) {
    print "\nArg value is $regex_value\n";
    var_dump(preg_match($regex_value, $subject));
}
$regex_value = new stdclass(); //Object
var_dump(preg_match($regex_value, $subject));
?>
--EXPECTF--

*** Testing preg_match() : error conditions ***

Arg value is abcdef

Warning: preg_match(): Delimiter must not be alphanumeric or backslash in %spreg_match_error1.php on line %d
bool(false)

Arg value is /[a-zA-Z]

Warning: preg_match(): No ending delimiter '/' found in %spreg_match_error1.php on line %d
bool(false)

Arg value is [a-zA-Z]/

Warning: preg_match(): Unknown modifier '/' in %spreg_match_error1.php on line %d
bool(false)

Arg value is /[a-zA-Z]/F

Warning: preg_match(): Unknown modifier 'F' in %spreg_match_error1.php on line %d
bool(false)

Arg value is Array

Warning: preg_match() expects parameter 1 to be string, array given in %spreg_match_error1.php on line %d
bool(false)

Arg value is /[a-zA-Z]/
int(1)

Warning: preg_match() expects parameter 1 to be string, object given in %spreg_match_error1.php on line %d
bool(false)
