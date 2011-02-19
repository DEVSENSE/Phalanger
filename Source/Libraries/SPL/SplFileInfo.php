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

class SplFileInfo {
  
  protected $fname = "";
  protected $pathInfo = array();
  
  function __construct($file_name) {
    $this->fname = $file_name;
    $this->pathInfo = pathinfo($this->fname);
  }

  function getPath() {
    return $this->pathInfo['dirname'];
  } 

  function getFilename() {
    return $this->pathInfo['basename'];
  } 

  function getFileInfo($class_name = NULL) {
    	
  }

  function getPathname() {
    return $this->pathInfo['dirname']."/".$this->pathInfo['basename'];  
  }

  function getPathInfo($class_name = NULL) {
    
  }

  function getPerms() {
    return fileperms($this->fname);
  }
    
  function getInode() {
   	return fileinode($this->fname);
  }

  function getSize() {
    return filesize($this->fname);	
  }

  function getOwner() {
    return fileowner($this->fname);
  }

  function getGroup() {
    return filegroup($this->fname);
  }

  function getATime() {
    return fileatime($this->fname);
  }

  function getMTime() {
    return filemtime($this->fname); 	
  }

  function getCTime() {
    return filectime($this->fname);
  }

  function getType() {
    	
  }

  function isWritable() {
  	return is_writable($this->fname);
  }

  function isReadable() {
  	return is_readable($this->fname);
  }

  function isExecutable() {
  	return is_executable($this->fname);
  }

  function isFile() {
    return is_dir($this->fname);
  }

  function isDir() {
    return is_dir($this->fname);	
  }

  function isLink() {
    return is_link($this->fname);
  }

  function __toString() {
    return $this->getPathname();     
  }

  function openFile($mode = 'r', $use_include_path = false, $context = NULL) { 
    
  }

  function setFileClass($class_name = "SplFileObject") {
  	    
  }

  function setInfoClass($class_name = "SplFileInfo") {
  	    
  }

}

?>