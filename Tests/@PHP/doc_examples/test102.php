[expect exact] 
1 2 3

[file]
<?php

// This is WRONG and will not work as desired.
if (1)
    include 'xyz.inc';
else
    include 'xyz.inc';


// This is CORRECT.
if (0) {
    include 'xyz.inc';
} else {
    include "xyz.inc";
}

echo "$x $y $z\n";

?>
