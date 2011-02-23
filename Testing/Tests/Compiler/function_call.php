[expect exact]
OK
[file]
<?
/*

  Checks whether args-aware routines returning by reference are called properly.

*/

class A
{
    static function &g()
    {
      // make args-aware:
      $x = func_get_args();
      
      return $x;
    }

    static function f()
    {
      $x = A::g(1,2);
      $y =& A::g(1,2);
    }
}

A::f();
echo "OK";
?>