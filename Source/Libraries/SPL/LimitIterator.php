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

class LimitIterator implements OuterIterator {
    
    private $it;
    private $offset;
    private $count;
    private $pos;

    function __construct(Iterator $it, $offset = 0, $count = -1)
    {
        if ($offset < 0) {
            throw new Exception('Parameter offset must be > 0');
        }
        if ($count < 0 && $count != -1) {
            throw new Exception('Parameter count must either be -1 or a value greater than or equal to 0');
        }
        $this->it     = $it;
        $this->offset = $offset;
        $this->count  = $count;
        $this->pos    = 0;
    }
    
    function seek($position) {
        if ($position < $this->offset) {
            throw new Exception('Cannot seek to '.$position.' which is below offset '.$this->offset);
        }
        if ($position > $this->offset + $this->count && $this->count != -1) {
            throw new Exception('Cannot seek to '.$position.' which is behind offset '.$this->offset.' plus count '.$this->count);
        }
        if ($this->it instanceof SeekableIterator) {
            $this->it->seek($position);
            $this->pos = $position;
        } else {
            while($this->pos < $position && $this->it->valid()) {
                $this->next();
            }
        }
    }

    function rewind()
    {
        $this->it->rewind();
        $this->pos = 0;
        $this->seek($this->offset);
    }
    
    function valid() {
        return ($this->count == -1 || $this->pos < $this->offset + $this->count)
             && $this->it->valid();
    }
    
    function key() {
        return $this->it->key();
    }

    function current() {
        return $this->it->current();
    }

    function next() {
        $this->it->next();
        $this->pos++;
    }

    function getPosition() {
        return $this->pos;
    }

    function getInnerIterator() {
        return $this->it;
    }

    function __call($func, $params) {
        return call_user_func_array(array($this->it, $func), $params);
    }
}

?>