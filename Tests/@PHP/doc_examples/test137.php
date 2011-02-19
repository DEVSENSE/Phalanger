[expect php]

[file]
<?php
abstract class AbstractClass {
   abstract public function test();
}

class ImplementedClass extends AbstractClass {
   public function test() {
       echo "ImplementedClass::test() called.\n";
   }
}

$o = new ImplementedClass;
$o->test();
?>
