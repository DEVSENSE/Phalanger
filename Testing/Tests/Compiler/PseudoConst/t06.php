[EXPECT] g

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
        static function g() { echo __FUNCTION__; }
      }
      B::g();
    }
    
    v();
  }
}

A::f();

?>
