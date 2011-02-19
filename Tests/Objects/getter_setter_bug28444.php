[expect php]
[file]
<?
	// always run against >= 5.0.4 because that is the version where
	// getter/setter behavior was somewhat changed

	class Object {
		public $x;
		function __construct($x) {
			$this->x = $x;
		}
	}

	class Overloaded {
		var $props;
		function __construct($x) {
			$this->x = new Object($x);
		}
		function __get($prop) {
			return $this->props[$prop];
		}
		function __set($prop, $val) {
			$this->props[$prop] = $val;
		}
	}
	$y = new Overloaded(2);

	echo $y->x->x, " "; // Prints 2...
	echo $y->x->x = 3; //Should print 3...
?>
