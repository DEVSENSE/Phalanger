[expect php]

[file]
<?

class foo
{
    function foo() {
    }
}

class TestClass {
   static $id = 0;

   function TestClass () {
        $this->id = 10;


	$neco = new foo();
	$neco->jo = $this;
	$neco->jo->id = 4;


	$prom = "id";
	$this->$prom = 5;
   }
}

$c = new TestClass ();

?>