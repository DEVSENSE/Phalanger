[EXPECT php]

[FILE]
<?
  class A
  {
    function f(){ echo "f";}
  }

  class B extends A
  {
    function f()
    {
      parent::f();
    }
  }
  
  $b = new B;
  $b->f();
?>
