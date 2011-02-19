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

class CachingIterator implements OuterIterator
{
    const CALL_TOSTRING        = 0x00000001;
    const CATCH_GET_CHILD      = 0x00000002;
    const TOSTRING_USE_KEY     = 0x00000010;
    const TOSTRING_USE_CURRENT = 0x00000020;

    private $it;
    private $current;
    private $key;
    private $valid;
    private $strValue;

    function __construct(Iterator $it, $flags = self::CALL_TOSTRING)
    {
        //Fixed some CIT_* constants to use class constants
        if ((($flags & self::CALL_TOSTRING) && ($flags & (self::TOSTRING_USE_KEY|self::TOSTRING_USE_CURRENT)))
        || (($flags & (self::TOSTRING_USE_KEY|self::TOSTRING_USE_CURRENT)) == (self::TOSTRING_USE_KEY|self::TOSTRING_USE_CURRENT)))
        {
            throw new InvalidArgumentException('Flags must contain only one of CALL_TOSTRING, TOSTRING_USE_KEY, TOSTRING_USE_CURRENT');
        }
        $this->it = $it;
        $this->flags = $flags & (0x0000FFFF);
        $this->next();
    }

    function rewind()
    {
        $this->it->rewind();
        $this->next();
    }
    
    function next()
    {
        if ($this->valid = $this->it->valid()) {
            $this->current = $this->it->current();
            $this->key = $this->it->key();
            if ($this->flags & self::CALL_TOSTRING) {
                if (is_object($this->current)) {
                    $this->strValue = $this->current->__toString();
                } else {
                    $this->strValue = (string)$this->current;
                }
            }
        } else {
            $this->current = NULL;
            $this->key = NULL;
            $this->strValue = NULL;
        }
        $this->it->next();
    }
    
    function valid()
    {
        return $this->valid;
    }

    function hasNext()
    {
        return $this->it->valid();
    }
    
    function current()
    {
        return $this->current;
    }

    function key()
    {
        return $this->key;
    }

    function __call($func, $params)
    {
        return call_user_func_array(array($this->it, $func), $params);
    }
    
    function __toString()
    {
        if ($this->flags & self::TOSTRING_USE_KEY)
        {
            return $this->key;
        }
        else if ($this->flags & self::TOSTRING_USE_CURRENT)
        {
            return $this->current;
        }
        if (!$this->flags & self::CALL_TOSTRING)
        {
            throw new exception('CachingIterator does not fetch string value (see CachingIterator::__construct)');
        }
        return $this->strValue;
    }
    
    function getInnerIterator()
    {
        return $this->it;
    }
}

?>