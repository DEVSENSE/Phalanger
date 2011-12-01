[expect php]
[file]
<?

function foo()
{
	$x = new ArrayIterator( array(1,2,3, 'a' => '4', 'b' => 5) );
	$x[] = 6;
	
	foreach ($x as $key => $value)
	{
		echo "$key => $value\n";
	}
}

foo();

?>