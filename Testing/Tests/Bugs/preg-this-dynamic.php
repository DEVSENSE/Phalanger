[expect php]
[file]
<?php
// Currently fails, because eval information isn't caputred while calling

class a
{
	function foo($p, $p2)
	{
		echo $p2.": ".$p."\n";
	}
	
	function a()
	{
		// tests whether local variables and $this are captured correctly (in dynamic invoke)
		$z="foo";
		$fn = 'preg_replace';
		$fn('!([a-z]+)!sie',"\$this->foo('\\1',\$z)","ahoj lidi rozkladame");
	}
}
new a();

// One more test - now using dynamic invoke
$x = 'bbb';
$fn = 'preg_replace';
echo $fn('!([a-z]+)!sie',"'\\1'.\$x","zz zz zz");
?>