[expect php]
[file]
<?php

error_reporting(0);

class pass {
	private function show() {
		echo "Call show()\n";
	}

	public function do_show() {
		$this->show();
	}
}

class fail extends pass {
	function do_show() {
		$this->show();
	}
}

$t = new pass();
$t->do_show();

$t2 = new fail();
$t2->do_show();

echo "Done\n"; // shouldn't be displayed
?>