[expect php]
[file]
<?php
include('Phalanger.inc');
class test {
  protected $x;

  static private $test = NULL;
  static private $cnt = 0;

  static function factory($x) {
    if (test::$test) {
      return test::$test;
    } else {
      test::$test = new test($x);
      return test::$test;
    }
  }

  protected function __construct($x) {
    test::$cnt++;
    $this->x = $x;
  }

  static function destroy() {
    test::$test = NULL;
  }

  protected function __destruct() {
  	test::$cnt--;
  }

  public function get() {
    return $this->x;
  }

  static public function getX() {
    if (test::$test) {
      return test::$test->x;
    } else {
      return NULL;
    }
  }
  
  static public function count() {
    return test::$cnt;
  }
}

echo "Access static members\n";
__var_dump(test::getX());
__var_dump(test::count());

echo "Create x and y\n";
$x = test::factory(1);
$y = test::factory(2);
__var_dump(test::getX());
__var_dump(test::count());
__var_dump($x->get());
__var_dump($y->get());

echo "Destruct x\n";
$x = NULL;
__var_dump(test::getX());
__var_dump(test::count());
__var_dump($y->get());

echo "Destruct y\n";
$y = NULL;
__var_dump(test::getX());
__var_dump(test::count());

echo "Destruct static\n";
test::destroy();
__var_dump(test::getX());

//commented out as it relies on deterministic destruction:
//__var_dump(test::count());

echo "Done\n";
?>