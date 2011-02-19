[expect exact] ahoj

[file]
<?php
// we have different behaviour here - we include only once.
require_once("function_foo.inc"); // this will include function_foo.inc
require_once("FUNCtion_foo.inc"); // PHP will include again on Windows!

foo("ahoj");

?>
