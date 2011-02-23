[EXPECT] v

[FILE]
<?
class A
{
  static function f()
  {
    function v()
    {
      echo __METHOD__;
    }
    
    v();
  }
}

A::f();

?>
