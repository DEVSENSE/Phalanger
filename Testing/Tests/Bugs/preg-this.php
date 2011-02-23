[expect php]
[file]
<?php
class a
{
	function foo($p, $p2)
	{
		echo $p2.": ".$p."\n";
	}
	
	function a()
	{
		// tests whether local variables and $this are captured correctly
		$z="foo";
		preg_replace('!([a-z]+)!sie',"\$this->foo('\\1',\$z)","ahoj lidi rozkladame");
	}
}
new a();

// test whether similar thing works in global context
$x = 'aaa';
echo preg_replace('!([a-z]+)!sie',"'\\1'.\$x","zz zz zz");

?>