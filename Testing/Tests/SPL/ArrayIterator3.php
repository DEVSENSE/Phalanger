[expect php]
[file]
<?

#test initial states

class ArrIt extends ArrayIterator
{
	function current()
	{
		echo __METHOD__  . count($this). '; ';
		return parent::current();
	}
	
	function valid()
	{
		//echo __METHOD__  . count($this). '; ';
		return parent::valid();
	}
	
	function key()
	{
		echo __METHOD__ . count($this) . '; ';
		return parent::key();
	}
	
	function rewind()
	{
		echo __METHOD__ . count($this) .'; ';
		return parent::rewind();
	}
	
	function next()
	{
		echo __METHOD__  . count($this). '; ';
		return parent::next();
	}
}

function foo()
{
	$a = new ArrIt(array('ka'=>'a', 'kb'=>'b'));
	var_dump($a->valid());
	var_dump($a->key());
	var_dump($a->current());
	
	$a = new ArrIt(array('ka'=>'a', 'kb'=>'b'));
	var_dump($a->key());
	var_dump($a->valid());
	var_dump($a->current());
	
	$a = new ArrIt(array('ka'=>'a', 'kb'=>'b'));
	var_dump($a->current());
	var_dump($a->valid());
	var_dump($a->key());
	
	$a = new ArrIt(array('ka'=>'a', 'kb'=>'b'));
	var_dump($a->current());
	var_dump($a->key());
	var_dump($a->valid());
	
	$a = new ArrIt(array('ka'=>'a', 'kb'=>'b'));
	var_dump($a->next());
	var_dump($a->current());
	var_dump($a->valid());
}

foo();

?>