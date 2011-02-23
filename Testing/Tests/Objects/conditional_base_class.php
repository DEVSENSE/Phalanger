[expect]
OK
[file]
<?
$c = true;

if ($c)
{
  class A { }
  
  class B extends A { }
  
  class C extends B { }  
}

echo "OK";
?>