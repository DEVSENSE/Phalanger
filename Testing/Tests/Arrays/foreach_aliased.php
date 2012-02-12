[expect php]
[file]
<?

function test()
{
	$arr = array(0,1,2,3);
	$first = true;	
	
	foreach ($arr as &$a)
	{
		if ($first)
		{
			$first = false;
			unset($arr[0]);
			unset($arr[3]);
			$arr[] = 4;
		}
		var_dump( $a );
	}
}
test();

?>