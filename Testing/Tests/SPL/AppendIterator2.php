[expect php]
[file]
<?php

class X extends AppendIterator
{
	function current()
	{
		echo __METHOD__ . '; ';
		return parent::current();
	}
	
	function valid()
	{
		echo __METHOD__ . '; ';
		return parent::valid();
	}
	
	function key()
	{
		echo __METHOD__ . '; ';
		return parent::key();
	}
	
	function rewind()
	{
		echo __METHOD__ . '; ';
		return parent::rewind();
	}
	
	function next()
	{
		echo __METHOD__ . '; ';
		return parent::next();
	}
}
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


$pizzas   = new ArrIt(array('Margarita', 'Siciliana', 'Hawaii'));
$toppings = new ArrIt(array('Cheese', 'Anchovies', 'Olives', 'Pineapple', 'Ham'));

$appendIterator = new X;
$appendIterator->append($pizzas);
$appendIterator->append($toppings);

foreach ($appendIterator as $key => $item) {
    echo "$key => $item", PHP_EOL;
}

$appendIterator->append($toppings);
while($appendIterator->valid())
{
    echo $appendIterator->key() . " => " . $appendIterator->current() . "\n";
    $appendIterator->next();
}

?>