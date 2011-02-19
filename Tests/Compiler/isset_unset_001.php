[expect php]
[file]
<?

function sp()
{
	return " <br>\n";
}


function f()
{
	echo "Testing x (object) in optimized function: ";
	echo isset($x).sp();
	$x = 5;
	echo isset($x).sp();
	unset($x);
	echo isset($x).sp();
}

function g()
{
	// no notice that $y is undefined is displayed, this is an optimized function

	echo "Testing x (PhpReference) in optimized function: ";
	echo isset($x).sp();
	$x = 5;
	echo isset($x).sp();
	unset($x);
	echo isset($x).sp();
	$$y =& $x;
}

function h()
{
	echo 'Testing $$x (created at runtime) in optimized function: ';
	$x = "y";
	echo isset($$x).sp();
	$$x = 5;
	echo isset($$x).sp();
	unset($$x);
}

echo "Testing x in global code: ";
echo isset($x).sp();
$x = 5;
echo isset($x).sp();
unset($x);
echo isset($x).sp();

f();
g();
h();

?>
