[expect exact]
001001
440
[file]
<?
echo is_numeric("+") ? "1":"0";
echo is_numeric(" ") ? "1":"0";
echo is_numeric("2.") ? "1":"0";
echo is_numeric("2.e") ? "1":"0";
echo is_numeric("2.e+") ? "1":"0";
echo is_numeric("2.e+1") ? "1":"0";
echo "\n";
$a = "44e1c";
echo (double)$a;
?>