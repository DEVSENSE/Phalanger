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

abstract class RecursiveFilterIterator extends FilterIterator implements RecursiveIterator
{

    private $ref;

    function __construct(RecursiveIterator $it)
    {
        parent::__construct($it);
    }
    
    function hasChildren()
    {
        return $this->getInnerIterator()->hasChildren();
    }



    function getChildren()
    {
/* Reflection is missing
        if(empty($this->ref))
        {
            $this->ref = new ReflectionClass($this);
        }
        return $this->ref->newInstance($this->getInnerIterator()->getChildren());
*/
    }


}



?>