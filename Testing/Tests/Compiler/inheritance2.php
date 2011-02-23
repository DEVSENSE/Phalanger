[expect] hey!

[file]
<?
eval ('class InEval { function f() { echo "hey!"; } }');

class A extends InEval
{
}

class B extends A
{
}

class C extends B
{
}

class D extends C
{
	function f()
	{
		return parent::f();
	}
}

D::f();

?>