[expect php]

[file]
<?
function foo()
{
  echo __FUNCTION__;
}

class bar
{
  function f()
  {
    echo __CLASS__;
    echo __METHOD__;
  }
}

foo();
$a = new bar();
$a->f();

?>