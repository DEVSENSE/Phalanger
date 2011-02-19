[expect php]
[file]

<?php
function foo()
{
	return FALSE;
}
if (foo()) 

{
	class A extends B {
		function test() { echo "eh"; }
	} 
}

else

{
	class A {
		function test() { echo "ok"; }
	} 
}


$a = 
new A;
$a->test();
?>