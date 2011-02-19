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

class ArrayObject implements IteratorAggregate, ArrayAccess, Countable {

	const ARRAY_AS_PROPS = 0x00000002;
	const STD_PROP_LIST = 0x00000001;
        
        private $array;
        private $type = "array";
        private $originalObject;
        private $flags;
        
        private $iterator_class = "ArrayIterator";

	/** Construct a new array iterator from anything that has a hash table.
	 * That is any Array or Object.
	 *
	 * @param $array the array to use.
	 */
	function __construct($array, $flags = 0, $iterator_class = "ArrayIterator") {
	  $this->flags = $flags;
	  $this->iterator_class = $iterator_class;
	
	  
	  if(is_object($array)) {
	    $this->array = (array)$array;
	    $this->type = "object";
	    //Maybe its needed for further operations...
	    $this->originalObject = $array;
	  } else {
	    $this->array = $array;
	  }

	}


	/** @return the iterator which is an ArrayIterator object connected to
	 * this object.
	 */
	function getIterator() {
	  return new $this->iterator_class($this->array);
	}


	/** @param $index offset to inspect
	 * @return whetehr offset $index esists
	 */	
	function offsetExists($index) {
	  return isset($this->array[$index]);
	}


	/** @param $index offset to return value for
	 * @return value at offset $index
	 */	
	function offsetGet($index) {
	  if($this->offsetExists($index)) {
            return $this->array[$index];
	  }
	}

	/** @param $index index to set
	 * @param $newval new value to store at offset $index
	 */	
	function offsetSet($index, $newval) {
	  $this->array[$index] = $newval;
	}

	/** @param $index offset to unset
	 */	
	function offsetUnset($index) {
	  unset($this->array[$index]);
	}

	/** @param $value is appended as last element
	 * @warning this method cannot be called when the ArrayObject refers to 
	 *          an object.
	 */	
	function append($value) {
	  if($this->type == "object") {
	    trigger_error("Have to emulate behavior of PHP ArrayObject. Original data was an object so use ArrayObject::offsetSet instead of ArrayObject::append().", E_USER_ERROR);
	  } else {
	    $this->array[] = $value;
	  }
	}

	/** @return a \b copy of the array
	 * @note when the ArrayObject refers to an object then this method 
	 *       returns an array of the public properties.
	 */	
	function getArrayCopy() {
	  return $this->array;
	}

	/** @return the number of elements in the array or the number of public
	 * properties in the object.
	 */
	function count() {
	  return count($this->array());
	}
	
	
	function setIteratorClass($iterator_class) {
	  $this->iterator_class = $iterator_class;
	}


	function getIteratorClass() {
	  return $this->iterator_class;
	}


}

?>