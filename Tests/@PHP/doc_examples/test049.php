[expect exact] 25

[file]
<?php

function test()
{
   return 25;
}

$bar = &test();
echo $bar;
?>
