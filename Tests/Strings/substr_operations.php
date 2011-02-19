[expect php]
[file]
<?

require('Phalanger.inc');


$var = "aaaaaaaa";

__var_dump(substr_replace($var, 'b', 0));
__var_dump(substr_replace($var, 'b', 0, strlen($var)));
__var_dump(substr_replace($var, 'b', 0, 0));
__var_dump(substr_replace($var, 'b', 10, -1));
__var_dump(substr_replace($var, 'b', -7, -1));
__var_dump(substr_replace($var, 'b', 10, -1));

echo "\n";

__var_dump(substr_count($var, 'a', 0));
__var_dump(substr_count($var, 'a', 0, strlen($var)));

__var_dump(@substr_count($var, null, 0, 0));
__var_dump(@substr_count($var, '', 0, 0));
__var_dump(@substr_count($var, 'a', -1, -1));
__var_dump(@substr_count($var, 'a', 3, 0));
__var_dump(@substr_count($var, 'a', 10, -1));
__var_dump(@substr_count($var, 'a', 6, 6));

echo "\n";

__var_dump(substr_replace(array("a" => $var,10 => $var,"b" => null, "", " "), 'b', -10, 5));

echo "\n";

__var_dump(substr_compare("abcde", "bc", 1, 2)); // 0
__var_dump(substr_compare("abcde", "bcg", 1, 2)); // 0
__var_dump(substr_compare("abcde", "BC", 1, 2, true)); // 0
__var_dump(substr_compare("abcde", "bc", 1, 3)); // 1
__var_dump(substr_compare("abcde", "cd", 1, 2)); // -1
?>