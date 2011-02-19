[expect php]

[file]
<?php
class MyCloneable {
   static $id = 0;

   function MyCloneable() {
       $this->id = self::$id++;
   }

   function __clone() {
       $this->address = "New York";
       $this->id = self::$id++;
   }
}

$obj = new MyCloneable();

$obj->name = "Hello";
$obj->address = "Tel-Aviv";

print $obj->id . "\n";

$obj_cloned = clone $obj;

print $obj_cloned->id . "\n";
print $obj_cloned->name . "\n";
print $obj_cloned->address . "\n";
?>
