[expect php]
[file]
<?php

class Circle {
	function draw() {
		echo "Circle\n";
	}
}

class Square {
	function draw() {
		echo "Square\n";
	}
}

function ShapeFactoryMethod($shape) {
	switch ($shape) {
		case "Circle":
			return new Circle();
		case "Square":
			return new Square();
	}
}

ShapeFactoryMethod("Circle")->draw();
ShapeFactoryMethod("Square")->draw();

?>