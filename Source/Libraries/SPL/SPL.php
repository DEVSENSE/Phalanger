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

function spl_autoload($class_name, $file_extensions = NULL) {}

 
function spl_autoload_call($class_name) {}

 
function spl_autoload_extensions($file_extensions) {}

 
function spl_autoload_functions() {}

 
function spl_autoload_register($autoload_function = "spl_autoload", $throw = true) {}


function spl_autoload_unregister($autoload_function = "spl_autoload") {}


function spl_classes() {

  return array(
      "AppendIterator" => "AppendIterator",
      "ArrayIterator" => "ArrayIterator",
      "ArrayObject" => "ArrayObject",
      "BadFunctionCallException" => "BadFunctionCallException",
      "BadMethodCallException" => "BadMethodCallException",
      "CachingIterator" => "CachingIterator",
      "Countable" => "Countable",
      "DirectoryIterator" => "DirectoryIterator",
      "DomainException" => "DomainException",
      "EmptyIterator" => "EmptyIterator",
      "FilterIterator" => "FilterIterator",
      "InfiniteIterator" => "InfiniteIterator",
      "InvalidArgumentException" => "InvalidArgumentException",
      "IteratorIterator" => "IteratorIterator",
      "LengthException" => "LengthException",
      "LimitIterator" => "LimitIterator",
      "LogicException" => "LogicException",
      "NoRewindIterator" => "NoRewindIterator",
      "OuterIterator" => "OuterIterator",
      "OutOfBoundsException" => "OutOfBoundsException",
      "OutOfRangeException" => "OutOfRangeException",
      "OverflowException" => "OverflowException",
      "ParentIterator" => "ParentIterator",
      "RangeException" => "RangeException",
      "RecursiveArrayIterator" => "RecursiveArrayIterator",
      "RecursiveCachingIterator" => "RecursiveCachingIterator",
      "RecursiveDirectoryIterator" => "RecursiveDirectoryIterator",
      "RecursiveFilterIterator" => "RecursiveFilterIterator",
      "RecursiveIterator" => "RecursiveIterator",
      "RecursiveIteratorIterator" => "RecursiveIteratorIterator",
      "RuntimeException" => "RuntimeException",
      "SeekableIterator" => "SeekableIterator",
//      "SimpleXMLIterator" => "SimpleXMLIterator",
      "SplFileInfo" => "SplFileInfo",
      "SplFileObject" => "SplFileObject",
      "SplObjectStorage" => "SplObjectStorage",
      "SplObserver" => "SplObserver",
      "SplSubject" => "SplSubject",
//      "SplTempFileObject" => "SplTempFileObject",
      "UnderflowException" => "UnderflowException",
      "UnexpectedValueException" => "UnexpectedValueException"
  );
}

 
function iterator_count(Traversable $it) {}

 
function iterator_to_array(Traversable $it) {}

?>