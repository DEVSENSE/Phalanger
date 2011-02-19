<?php

/*
Copied from PHP 5 implementation (http://www.php.net/~helly/php/ext/spl/).

The use and distribution terms for this software are contained in the file
named License.txt,
which can be found in the root of the Phalanger distribution. By using this
software
in any fashion, you are agreeing to be bound by the terms of this license.
You must not remove this notice from this software.

*/

class IteratorIterator implements OuterIterator {
	
  private $iterator;

  function __construct($iterator, $classname = null) {
    if($iterator instanceof IteratorAggregate) {
      $iterator = $iterator->getIterator();
    }
    
    if($iterator instanceof Iterator) {
      $this->iterator = $iterator;
    } else {
      throw new Exception("Classes that only implement Traversable can be wrapped only after converting class IteratorIterator into c code");
    }
  }

  function getInnerIterator() {
    return $this->iterator;
  }

  function valid() {
    return $this->iterator->valid();
  }

  function key() {
    return $this->iterator->key();
  }

  function current() {
    return $this->iterator->current();
  }

  function next() {
    return $this->iterator->next();
  }

  function rewind() {
    return $this->iterator->rewind();
  }

  function __call($func, $params) {
    return call_user_func_array(array($this->it, $func), $params);
  }

}

?>