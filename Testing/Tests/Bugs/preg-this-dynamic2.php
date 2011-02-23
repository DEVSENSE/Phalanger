[expect php]
[file]
<?php
// Currently fails, because $this reference is not caputred

class a
{
	function foo($p, $p2)
	{
		echo $p2.": ".$p."\n";
	}
	
	function a()
	{
		// the first call causes compiler to capture eval info
		$z="foo";
		preg_replace('!([a-z]+)!sie',"\$this->foo('\\1',\$z)","ahoj lidi rozkladame");
		
		// but this reference isn't captured :-(
		$fn = 'preg_replace';
		$fn('!([a-z]+)!sie',"\$this->foo('\\1',\$z)","ahoj lidi rozkladame");
	}
}
new a();
?>