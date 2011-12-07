[expect php]
[file]
<?php
// This iterator filters all values with less than 10 characters
class LengthFilterIterator extends FilterIterator {

    public function accept() {
        echo "accept(".parent::current()."), ";
		// Only accept strings with a length of 10 and greater
		return strlen(parent::current()) > 10;
    }
	
	public function next()
	{
		echo "next(), ";
		return parent::next();
	}
	
	public function valid()
	{
		echo "valid(), ";
		return parent::valid();
	}
	
	public function rewind()
	{
		echo "rewind(), ";
		return parent::rewind();
	}

}

$arrayIterator = new ArrayIterator(array('test1', 'more than 10 characters', 'another more than 10 characters', "test2", 'test3', 'someting longer than 10 chars'));
$lengthFilter = new LengthFilterIterator($arrayIterator);

$lengthFilter->rewind();

echo "\n";

foreach ($lengthFilter as $value) {
    echo "$value, ";
}
?>