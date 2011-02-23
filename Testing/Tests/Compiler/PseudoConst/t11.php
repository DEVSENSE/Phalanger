[expect] g
[file]
<?
  function f($a)
  {
    echo $a;
  }
  
  function g()
  {
    f(__FUNCTION__);
  }
  
  g();
?>
