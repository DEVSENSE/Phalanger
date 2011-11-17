[expect php]
[file]
<?php

class Foo{

}

class Bar{
	var $x = 1;
}

$x = new Foo();
$y = new Bar();

var_dump(!$x);
var_dump(!$y);

var_dump(-$x);
var_dump(-$y);

var_dump(+$x);
var_dump(+$y);

// Phalanger doesn't want to support this
// var_dump($x++);
// var_dump($y++);
// var_dump(++$x);
// var_dump(++$y);


var_dump( $x + 1);
var_dump( $y + 1);

var_dump( $x + 1.1);
var_dump( $y + 1.1);

var_dump( $x + $y);



var_dump( $x - 1);
var_dump( $y - 1);

var_dump( $x - 1.1);
var_dump( $y - 1.1);

var_dump( $x - $y);

var_dump( $x * 1);
var_dump( $y * 1);

var_dump( $x * 1.1);
var_dump( $y * 1.1);

var_dump( $x * $y);

// var_dump( $x / 1);
// var_dump( $y / 1);

// var_dump( $x / 1.1);
// var_dump( $y / 1.1);

// var_dump( $x / $y);

// var_dump( $x % 1);
// var_dump( $y % 1);

// var_dump( $x % 1.1);
// var_dump( $y % 1.1);

// var_dump( $x % $y);


?>