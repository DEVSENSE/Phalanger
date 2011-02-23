[expect exact]
test 1
test 2

[file]
<?php

if (1==1) {
	class b {
	}
}

// function 'foo' inside incomplete class 'unk1'
class unk1 extends b {
  function run() {
		function foo1() { echo "test 1\n"; }
	}
}

// class 'a' inside incomplete class 'unk2'
// can't compile 'a' and its methods
class unk2 extends b {
  function run() {
		class a {
				function run() {
					function foo2() { echo "test 2\n"; }
				}
		}
	}
}

$s = new unk1;
$s->run();
foo1();

$s = new unk2;
$s->run();
$a = new a;
$a->run();
foo2(); 
?>