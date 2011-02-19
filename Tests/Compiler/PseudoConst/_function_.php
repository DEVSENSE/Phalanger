[expect php]
[file]

<?
  function f()
  {
    class A
    {
      var $a = __FUNCTION__;
      
      function g()
      {
        echo $this->a;
        echo __FUNCTION__;
      }
    } 
    
    $a = new A;
    $a->g();
  }

  f();

?>
