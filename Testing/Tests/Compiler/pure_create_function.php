[expect php]

[file]
<?

function foo()
{
	$x = create_function('$a, $b', 'echo $a. $b;');
	
	$x("a", "b");
}

foo();

?>