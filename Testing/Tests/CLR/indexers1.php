[clr]
[expect exact]
OK
[file]
<?
	function Read($hashtable) {
		$h = $hashtable["key"];	// $h has to be wrapped into ClrObject!
		$m = $h["message"];		// GetItem operator checks if $h is empty => any non-PHP object causes an exception
	}

	function test() {
		
		$x = new System\Collections\Hashtable();
		$y = new System\Collections\Hashtable();

		$x->Add("key", $y);
		$y->Add("message", $y);
		
		Read($x);
	}
	test();
?>OK