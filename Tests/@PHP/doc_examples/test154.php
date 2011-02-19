[expect php]

[file]
<?php
// These examples are specific to using arrays inside of strings.
// When outside of a string, always quote your array string keys 
// and do not use {braces} when outside of strings either.

error_reporting(E_ERROR);

$fruits = array('strawberry' => 'red', 'banana' => 'yellow');

// Works but note that this works differently outside string-quotes
echo "A banana is $fruits[banana].";

// Works
echo "A banana is {$fruits['banana']}.";

// Works but PHP looks for a constant named banana first
// as described below.
echo @"A banana is {$fruits[banana]}.";

// Won't work, use braces.  This results in a parse error.
//echo "A banana is $fruits['banana'].";

// Works
echo "A banana is " . $fruits['banana'] . ".";

$square->width = 1;

// Works
echo "This square is $square->width meters broad.";

// Won't work. For a solution, see the complex syntax.
//echo "This square is $square->width00 centimeters broad.";
?>
