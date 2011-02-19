[expect]
OK
[file]
<?
eval('class A { function A(&$x) { } }');

class B extends A { }

$mh = 1;
new B($mh);

echo "OK";
?>