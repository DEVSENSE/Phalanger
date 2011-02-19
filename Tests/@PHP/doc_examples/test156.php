[expect] Notice

[file]
<?php
define("CONSTANT", "Hello world.");
//echo CONSTANT; // outputs "Hello world."
echo Constant; // outputs "Constant" and issues a notice.
?>
