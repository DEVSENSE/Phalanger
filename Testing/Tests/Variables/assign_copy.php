[expect php]
[file]
<?  

eval("
function bar()
{
	return array(1 => 'x');
}
");

function foo()
{
    $a = $b = bar();
    //$b = bar();
    //$a = $b;

    $a[1] = 'y';
	var_dump($a != $b);
}

foo();

?>