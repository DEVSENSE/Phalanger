[expect php]
[file]
<?

function my_autoload($name)
{

	eval('
		class ' . $name . '
		{
			function __construct($param){ echo $param; }
			function foo(){ echo "foo"; }	
		}
		'
	);

}

spl_autoload_register( "my_autoload" );

$x = new AnyClass( "hello world" );
$x->foo();

?>