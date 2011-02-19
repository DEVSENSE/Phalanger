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

abstract class FilterIterator implements OuterIterator
{
    private $it;

    function __construct(Iterator $it) {
        $this->it = $it;
    }

    function rewind() { 
        $this->it->rewind();
        $this->fetch();
    }

    abstract function accept();

    protected function fetch() {
        while ($this->it->valid()) {
            if ($this->accept()) {
                return;
            }
            $this->it->next();
        };
    }

    function next() {
        $this->it->next();
        $this->fetch();
    }
    
    function valid() {
        return $this->it->valid();
    }
    
    function key() {
        return $this->it->key();
    }
    
    function current() {
        return $this->it->current();
    }
    
    protected function __clone() {
        // disallow clone 
    }

    function getInnerIterator()
    {
        return $this->it;
    }

    function __call($func, $params)
    {
        return call_user_func_array(array($this->it, $func), $params);
    }
}

?>