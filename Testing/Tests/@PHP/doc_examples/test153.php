[expect php]

[file]
<?php
// Let's show all errors
error_reporting(E_ALL);

$arr = array('fruit' => 'apple', 'veggie' => 'carrot');

// Correct
print $arr['fruit'];  // apple
print $arr['veggie']; // carrot

// Incorrect.  This works but also throws a PHP error of
// level E_NOTICE because of an undefined constant named fruit
// 
// Notice: Use of undefined constant fruit - assumed 'fruit' in...
//print $arr[fruit];    // apple

// Let's define a constant to demonstrate what's going on.  We
// will assign value 'veggie' to a constant named fruit.
define('fruit', 'veggie');

// Notice the difference now
print $arr['fruit'];  // apple
print $arr[fruit];    // carrot

// The following is okay as it's inside a string.  Constants are not
// looked for within strings so no E_NOTICE error here
print "Hello $arr[fruit]";      // Hello apple

// With one exception, braces surrounding arrays within strings
// allows constants to be looked for
print "Hello {$arr[fruit]}";    // Hello carrot
print "Hello {$arr['fruit']}";  // Hello apple

// This will not work, results in a parse error such as:
// Parse error: parse error, expecting T_STRING' or T_VARIABLE' or T_NUM_STRING'
// This of course applies to using autoglobals in strings as well
//print "Hello $arr['fruit']";
//print "Hello $_GET['foo']";

// Concatenation is another option
print "Hello " . $arr['fruit']; // Hello apple
?>
