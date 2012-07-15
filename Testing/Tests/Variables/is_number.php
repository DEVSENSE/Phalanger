[expect php]
[file]
<?
function test($x)
{
	echo is_numeric($x) ? "1":"0";
}

foreach (array("+", " ", "2.", "2.e", "2.e+", "2.e+1", ".e2", ".", "1e", "-e2") as $x)
	test($x);

echo "\n";
$a = "44e1c";
echo (double)$a;
?>