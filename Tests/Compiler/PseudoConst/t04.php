[EXPECT] B

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
        static function g() { echo __CLASS__; }
      }
      B::g();
    }
    
    v();
  }
}

A::f();

?>
