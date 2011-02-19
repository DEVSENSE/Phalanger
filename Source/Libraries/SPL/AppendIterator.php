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

//NOT TESTED OR MODIFIED YET

class AppendIterator implements OuterIterator
{
    private $iterators;

    function __construct()
    {
        $this->iterators = new ArrayIterator();
    }

    function append(Iterator $it)
    {
        $this->iterators->append($it);
    }

    function getInnerIterator()
    {
        return $this->iterators->current();
    }

    function rewind()
    {
        $this->iterators->rewind();
        if ($this->iterators->valid())
        {
            $this->getInnerIterator()->rewind();
        }
    }

    function valid()
    {
        return $this->iterators->valid() && $this->getInnerIterator()->valid();
    }

    function current()
    {
        /* Using $this->valid() would be exactly the same; it would omit
         * the access to a non valid element in the inner iterator. Since
         * the user didn't respect the valid() return value false this
         * must be intended hence we go on. */
        return $this->iterators->valid() ? $this->getInnerIterator()->current() : NULL;
    }

    function key()
    {
        return $this->iterators->valid() ? $this->getInnerIterator()->key() : NULL;
    }

    function next()
    {
        if (!$this->iterators->valid())
        {
            return; /* done all */
        }
        $this->getInnerIterator()->next();
        if ($this->getInnerIterator()->valid())
        {
            return; /* found valid element in current inner iterator */
        }
        $this->iterators->next();
        while ($this->iterators->valid())
        {
            $this->getInnerIterator()->rewind();
            if ($this->getInnerIterator()->valid())
            {
                return; /* found element as first elemet in another iterator */
            }
            $this->iterators->next();
        }
    }

    function __call($func, $params)
    {
        return call_user_func_array(array($this->getInnerIterator(), $func), $params);
    }
}

?>