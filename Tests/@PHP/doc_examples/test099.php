[expect php]

[file]
<?php

$a = 1;
$name = "a.inc";

require 'a.inc';
require $name;
require ('a.inc');

?>
