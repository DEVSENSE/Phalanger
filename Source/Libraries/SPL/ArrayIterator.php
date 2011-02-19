<?php

/*
Copyright (c) 2006 Daniel Henning. Based on PHP 5 implementation (http://www.php.net/~helly/php/ext/spl/).

The use and distribution terms for this software are contained in the file
named License.txt,
which can be found in the root of the Phalanger distribution. By using this
software
in any fashion, you are agreeing to be bound by the terms of this license.
You must not remove this notice from this software.
*/

class ArrayIterator implements SeekableIterator, ArrayAccess, Countable {
        
        const STD_PROP_LIST = 1;
        const ARRAY_AS_PROPS = 2;


	private $array = array();
		

  	public function __construct($array = null) {
  	 	if(is_null($array)) {
  	 	  $this->array = array();
  	 	} else {
		  $this->array = $array;
		}
		  
	}


	function offsetExists($index) 
	{
	}



	function offsetGet($index)
	{
	}


	function offsetSet($index, $newval) {
	}

	
	function offsetUnset($index) {
	}

	
	function append($value) {
	}

	
	function getArrayCopy() {
	  return $this->array();
	}

	
	function seek($position)
	{
	}

	
	function count() {
	  return count($this->array);
	}
	
	
	
	// iterator:
	function next()
	{
	}
	
	function key()
	{
	}
	
	function current()
	{
	}
	
	function valid()
	{
	}
	
	function rewind()
	{
	}


}

?>