[EXPECT]

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
      echo __CLASS__;
    }
    
    v();
  }
}

A::f();

?>
