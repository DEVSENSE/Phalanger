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

class InfiniteIterator extends IteratorIterator {

  function next() {
    $this->getInnerIterator()->next();
    if(!$this->getInnerIterator()->valid()) {
      $this->getInnerIterator()->rewind();
    }
  }
}

?>