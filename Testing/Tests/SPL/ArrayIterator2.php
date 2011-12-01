[expect php]
[file]
<?

class ArrayCallbackIterator extends ArrayIterator { 
  private $callback; 
  public function __construct($value, $callback) { 
    parent::__construct($value); 
    $this->callback = $callback; 
  }

  public function current() { 
    $value = parent::current(); 
    return call_user_func($this->callback, $value); 
  }
} 

function markinate($value)
{
	return "[[$value]]\n";
}

function foo()
{
	$it = new ArrayCallbackIterator(array("hello", "world"), "markinate");
	foreach ($it as $markinated)
		echo $markinated;
}

foo();

?>