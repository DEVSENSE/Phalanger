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

class DirectoryIterator extends SplFileInfo implements Iterator {
    
    protected $entry = NULL;
    protected $dirObject;
    protected $path;

    function __construct($path) {
      if(!file_exists($path)) {
      	throw new RuntimeException("Path $path not found");
      }
      parent::__construct($path);
    
      $this->path = $path;
    
      $this->dirObject = dir($path);
      $this->next();
    }

    function rewind() {
      $this->dirObject->rewind();
    }
    

    function valid() {
      
      if(is_null($this->entry)) {
      	$this->next();
      }
   
      if($this->entry) {
        return true;
      }
      return false;
    }


    function key() {
      return NULL;
    }


    function current() {
      return $this->entry;
    }


    function next() {
      $this->entry = $this->dirObject->read();
      if($this->entry) {
      	parent::__construct($this->entry);        
      }
    }


    function isDot() {
      if($this->entry == "." || $this->entry == "..") {
      	return true;
      }
      return false;
    } 


    function __toString() {
      return (string)$this->entry;
    }

}

?>