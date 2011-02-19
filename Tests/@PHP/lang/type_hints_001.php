[expect php]
[file]
<?php


class Foo {
}

class Bar {
}

function type_hint_foo(Foo $a) {
echo "!!!";
}

$foo = new Foo;
$bar = new Bar;

error_reporting(0);
type_hint_foo($foo);
type_hint_foo($bar);

?>
