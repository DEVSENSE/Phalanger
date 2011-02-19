[expect php]
[file]
<?

function my_autoload($name)
{

	eval('
		class ' . $name . '
		{
			function bar(){ echo "bar"; }	
		}
		'
	);

}

spl_autoload_register( "my_autoload" );

//
// note
// the class must be declared within an inclusion,
// because class declaration is performed before any other code,
// so in case it would be in the same file as spl_autoload_register,
// it sould be performed before autoload initialization
//
// same in PHP
//
include "Autoload_extends.inc";

$x = new XXX();
$x->foo();
$x->bar();

?>