[EXPECT php]

[FILE]
<?php
  class A
  {
    function f(){ echo f;}
  }

  class B extends A
  {
    function g()
    {
     echo "g";
    }
    
    function f()
    {
      self::g();
    }
  }
  
  $b = new B;
  $b->f();
?>
