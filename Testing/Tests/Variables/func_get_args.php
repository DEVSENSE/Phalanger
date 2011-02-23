[expect php]
[file]
<?
/*

  Checks whether references are correctly dereferenced when calling args-aware function by name.
 
*/

function f($a)
{
  $args = func_get_args();
  var_dump($args);
  $args[0] = 4;
}

function g()
{
  $a = 1;
  $f = "f";
  $f($a);
  var_dump($a);
}

g();
?>