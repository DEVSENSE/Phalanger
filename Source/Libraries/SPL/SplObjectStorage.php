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

class SplObjectStorage implements Iterator, Countable {

  private $storage = array();
  private $index = 0;

  function rewind() {
    rewind($this->storage);
  }
  
  function valid() {
    return key($this->storage) !== false;
  }
  
  function key() {
    return $this->index;
  }
  
  function current() {
    return current($this->storage);
  }

  function next() {
    next($this->storage);
    $this->index++;
  }

  function count() {
    return count($this->storage);
  }

  function contains($obj) {
    if(is_object($obj)) {
      foreach($this->storage as $object) {
        if($object === $obj) {
          return true;
        }
      }
    }
    return false;
  }

  function attach($obj) {
    if(is_object($obj) && !$this->contains($obj)) {
      $this->storage[] = $obj;
    }
  }

  function detach($obj) {
    if(is_object($obj)) {
      foreach($this->storage as $idx => $object) {
        if($object === $obj) {
          unset($this->storage[$idx]);
          $this->rewind();
          return;
        }
      }
    }
  }

}

?>