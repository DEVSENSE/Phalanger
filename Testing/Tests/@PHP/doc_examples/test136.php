[expect php]

[file]
<?php

error_reporting(E_ALL & ~E_NOTICE);

class MyClass {
   public    $public     = "MyClass::public!\n";
   protected $protected  = "MyClass::Protected!\n";
   protected $protected2 = "MyClass::Protected2!\n";
   private   $private    = "MyClass::private!\n";

   function printHello() {
      print "MyClass::printHello() " . $this->private;
      print "MyClass::printHello() " . $this->protected;
      print "MyClass::printHello() " . $this->protected2;
   }
}

class MyClass2 extends MyClass {
   protected $protected = "MyClass2::protected!\n";

   function printHello() {

      MyClass::printHello();    

      print "MyClass2::printHello() " . $this->public; 
      print "MyClass2::printHello() " . $this->protected; 
      print "MyClass2::printHello() " . $this->protected2;

      /* Will result in a Fatal Error: */
      //print "MyClass2::printHello() " . $this->private; /* Fatal Error */

   }
}

$obj = new MyClass();

print "Main:: " . $obj->public;
//print $obj->private; /* Fatal Error */
//print $obj->protected;  /* Fatal Error */
//print $obj->protected2;  /* Fatal Error */

$obj->printHello(); /* Should print */

$obj2 = new MyClass2();
print "Main:: " . $obj2->private; /* Undefined */ 

//print $obj2->protected;   /* Fatal Error */ 
//print $obj2->protected2;  /* Fatal Error */ 

$obj2->printHello();
?>
