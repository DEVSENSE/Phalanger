[expect php]
[file]
<?
class A 
{
}

function f(A $a = null)
{
  var_dump($a);
}

function g(A $a)
{
  var_dump($a);
}

function h(array $a = null)
{
  var_dump($a);
}

function r(A &$a = null)
{
  var_dump($a);
}

function s(A &$a)
{
  var_dump($a);
}
  
f();  
f(null);  
h();  
h(null);  
r();  
r($x = null); 
$f = "f";
$f();
$f(null);
$r = "r";
$r();
$r($x = null);
//g();     // error
//g(null); // error
//s();     // error
//s($x = null); // error
?>