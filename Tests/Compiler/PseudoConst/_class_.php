[EXPECT]

[FILE]
<?
  class A
  {
    function f()
    {
      function g()
      {
        echo __CLASS__;
      }
      g();
    }
  }

  $a = new A;
  $a->f();
?>
