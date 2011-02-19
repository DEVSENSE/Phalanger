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

class RecursiveDirectoryIterator extends DirectoryIterator implements RecursiveIterator {

  const CURRENT_AS_FILEINFO =  0x00000010; /* make RecursiveDirectoryTree::current() return SplFileInfo */
  const KEY_AS_FILENAME     =  0x00000020; /* make RecursiveDirectoryTree::key() return getFilename() */
  const NEW_CURRENT_AND_KEY =  0x00000030; /* CURRENT_AS_FILEINFO + KEY_AS_FILENAME */

  protected $flags;

  function __construct($path, $flags = 0) {

    parent::__construct($path);
    $this->flags = $flags;
  }

/*
  function key() {

  }
*/

  function current() {
    return $this->path."/".$this->entry;
  }


  function hasChildren() {
    if($this->current() == "." || $this->current() == "..") {
      return false;
    }
    return is_dir($this->current);
  }


  function getChildren() {
  
  }


  function getSubPath() {
  	
  }

  function getSubPathname() {
  	
  }

  function getSubPathInfo($class_name = NULL) {

    if(is_null($class_name)) {	
      return new SplFileInfo($this->entry);
    }

    return new $class_name($this->entry);

  }

}

?>