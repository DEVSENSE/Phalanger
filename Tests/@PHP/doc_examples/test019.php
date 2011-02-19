[expect php]

[file]
<?php
// Get the first character of a string
$str = 'This is a test.';
$first = $str{0};
echo "$first ";

// Get the third character of a string
$third = $str{2};
echo "$third ";

// Get the last character of a string.
$str = 'This is still a test.';
$last = $str{strlen($str)-1}; 
echo "$last ";

// Modify the last character of a string
$str = 'Look at the sea';
$str{strlen($str)-1} = 'e';
echo "$str ";
          
?>
