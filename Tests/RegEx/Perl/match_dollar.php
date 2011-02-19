[expect php]
[file]
<?
$p = '/([a-z]+)$/D';
$s = "more info";
preg_match($p,$s,$m); var_dump(@$m[1]);

$p = '/([a-z]+)$/D';
$s = "more info\n";
preg_match($p,$s,$m); var_dump(@$m[1]);

$p = '/([a-z]+)$/D';
$s = "more info\r\n";
preg_match($p,$s,$m); var_dump(@$m[1]);

$p = '/([a-z]+)$/D';
$s = "more info\r";
preg_match($p,$s,$m); var_dump(@$m[1]);

echo "---------\n";

$p = '/([a-z]+)$/';
$s = "more info";
preg_match($p,$s,$m); var_dump(@$m[1]);

$p = '/([a-z]+)$/';
$s = "more info\n";
preg_match($p,$s,$m); var_dump(@$m[1]);

$p = '/([a-z]+)$/';
$s = "more info\r\n";
preg_match($p,$s,$m); var_dump(@$m[1]);

$p = '/([a-z]+)$/';
$s = "more info\r";
preg_match($p,$s,$m); var_dump(@$m[1]);
?>