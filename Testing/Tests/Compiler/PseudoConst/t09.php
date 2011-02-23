[EXPECT] v

[FILE]
<?
class A
{
  static function f()
  {
    function v()
    {
      class B
      {
        
      }
      echo __FUNCTION__;
    }
    
    v();
  }
}

A::f();

?>
