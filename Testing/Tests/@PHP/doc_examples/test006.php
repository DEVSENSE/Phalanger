[expect php]
[file]
<?php
$bool = TRUE;   // a bool
$str  = "foo";  // a string
$int  = 12;     // an integer

echo gettype($bool); // prints out "bool"
echo gettype($str);  // prints out "string"

// If this is an integer, increment it by four
if (is_int($int)) {
    $int += 4;
}
echo $int;

// If $bool is a string, print it out
// (does not print out anything)
if (is_string($bool)) {
    echo "string: $bool";
}
?>
