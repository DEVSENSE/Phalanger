[EXPECT] B

[FILE]
<?
class A
{
  static function f()
  {
    class B
    {
      static function g() { echo __CLASS__; }
    }
    B::g();
  }
}

A::f();

?>
