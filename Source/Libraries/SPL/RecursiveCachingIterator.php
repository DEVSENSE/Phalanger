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

class RecursiveCachingIterator extends CachingIterator implements RecursiveIterator
{
    private $hasChildren;
    private $getChildren;
    private $ref;

    function __construct(RecursiveIterator $it, $flags = self::CALL_TOSTRING)
    {
        parent::__construct($it, $flags);
    }

    function rewind()
    {
       $this->hasChildren = false;
       $this->getChildren = NULL;
       parent::rewind();
    }

    function next()
    {
        if ($this->hasChildren = $this->it->hasChildren())
        {
            try
            {
                $child = $this->it->getChildren();
                if (!$this->ref)
                {
                  //TODO:
                  //  $this->ref = new ReflectionClass($this);
                }
                //$this->getChildren = $ref->newInstance($child, $this->flags);
            }
            catch(Exception $e)
            {
                if (!$this->flags & self::CATCH_GET_CHILD)
                {
                    throw $e;
                }
                $this->hasChildren = false;
                $this->getChildren = NULL;
            }
        } else
        {
            $this->getChildren = NULL;
        }
        parent::next();
    }
    
    function hasChildren()
    {
        return $this->hasChildren;
    }

    function getChildren()
    {
        return $this->getChildren;
    }
}

?>