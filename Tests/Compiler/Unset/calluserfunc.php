[expect php]
[file]
<?php
$x = 10;
call_user_func('unset', $x);
var_dump($x);

?>