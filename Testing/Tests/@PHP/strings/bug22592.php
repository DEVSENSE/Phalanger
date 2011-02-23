[expect php]
[file]
<?php
$wrong = $correct = 'abcdef';

$t = $x[] = 'x';

echo($correct),"\n";
echo($wrong),"\n";

$correct[1] = '*';
$correct[3] = '*';
$correct[5] = '*';

// This produces the 
$wrong[1] = $wrong[3] = $wrong[5] = '*';

echo ($correct),"\n";
echo ($wrong),"\n";

?>