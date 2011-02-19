[expect php]
[file]
<?
	error_reporting(E_ALL & ~E_NOTICE);

	// note: test agains PHP >= 5.1 where Serializable was introduced
	class C
	{
		var $a = "a";
		var $b = "b";
	
		function __sleep()
		{
			echo "__sleep\n";
			return array("b", "are_you_kidding");
		}

		function __wakeup()
		{
			echo "__wakeup\n";
		}
	}
	
	class D implements Serializable
	{
		function serialize()
		{
			echo "serialize\n";
			return "coo-koo";
		}
		
		function unserialize($x)
		{
			echo "unserialize\n";
			echo "$x\n";
		}
	}

	$x = new C;
	$x->a = "new_a";
	$x->b = "new_b";
	$y = serialize($x);
	echo "$y\n";
	$x = unserialize($y);
	echo "$x->a\n";
	echo "$x->b\n";
	
	$x = new D;
	$y = serialize($x);
	$x = unserialize($y);
?>
