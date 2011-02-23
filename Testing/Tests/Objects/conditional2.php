[expect exact] AB

[file]
<?
class B 
{ 
  static function f() 
  { echo "B"; } 
}

class A extends B 
{ 
  static function f() 
  { 
    echo "A"; 
    eval('parent::f();'); 
  } 
}

A::f();

?>
