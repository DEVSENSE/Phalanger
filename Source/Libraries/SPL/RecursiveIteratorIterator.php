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

class RecursiveIteratorIterator implements OuterIterator
{
    const LEAVES_ONLY       = 0;
    const SELF_FIRST        = 1;
    const CHILD_FIRST       = 2;

    const CATCH_GET_CHILD   = 0x00000002;

    private $ait   = array();
    private $level = 0;
    private $mode  = self::LEAVES_ONLY;
    private $flags = 0;
    private $max_depth = -1;

    function __construct(RecursiveIterator $it, $mode = self::LEAVES_ONLY, $flags = 0)
    {
        $this->ait[0] = $it;
        $this->mode   = $mode;
        $this->flags  = $flags;
    }

    function rewind()
    {
        while ($this->level) {
            unset($this->ait[$this->level--]);
            $this->endChildren();
        }
        $this->ait[0]->rewind();
        $this->ait[0]->recursed = false;
        $this->callNextElement(true);
    }
    
    function valid()
    {
        $level = $this->level;
        while ($level >= 0) {
            $it = $this->ait[$level];
            if ($it->valid()) {
                return true;
            }
            $level--;
            $this->endChildren();
        }
        return false;
    }
    
    function key()
    {
        $it = $this->ait[$this->level];
        return $it->key();
    }
    
    function current()
    {
        $it = $this->ait[$this->level];
        return $it->current();
    }
    
    function next()
    {
        while ($this->level >= 0) {
            $it = $this->ait[$this->level];
            if ($it->valid()) {
                if (!$it->recursed && $this->callHasChildren()) {
                    if ($this->max_depth == -1 || $this->max_depth > $this->level) {
                        $it->recursed = true;
                        try
                        {
                            $sub = $this->callGetChildren();
                        }
                        catch (Exception $e)
                        {
                            if (!($this->flags & self::CATCH_GET_CHILD))
                            {
                                throw $e;
                            }
                            $it->next();
                            continue;
                        }
                        $sub->recursed = false;
                        $sub->rewind();
                        if ($sub->valid()) {
                            $this->ait[++$this->level] = $sub;
                            if (!$sub instanceof RecursiveIterator) {
                                throw new Exception(get_class($sub).'::getChildren() must return an object that implements RecursiveIterator');
                            }
                            $this->beginChildren();
                            return;
                        }
                        unset($sub);
                    }
                    else
                    {
                        /* do not recurse because of depth restriction */
                        if ($this->flages & self::LEAVES_ONLY)
                        {
                            $it->next();
                            continue;
                        }
                        else
                        {
                            return; // we want the parent
                        }
                    }
                    $it->next();
                    $it->recursed = false;
                    if ($it->valid()) {
                        return;
                    }
                    $it->recursed = false;
                }
            }
            else if ($this->level > 0) {
                unset($this->ait[$this->level--]);
                $it = $this->ait[$this->level];
                $this->endChildren();
                $this->callNextElement(false);
            }
        }
        $this->callNextElement(true);
    }

    function getSubIterator($level = NULL)
    {
        if (is_null($level)) {
            $level = $this->level;
        }
        return @$this->ait[$level];
    }

    function getInnerIterator()
    {
        return $this->it;
    }

    function getDepth()
    {
        return $this->level;
    }

    function callHasChildren()
    {
        return $this->ait[$this->level]->hasChildren();
    }

    function callGetChildren()
    {
        return $this->ait[$this->level]->getChildren();
    }

    function beginChildren()
    {
    }
    
    function endChildren()
    {
    }

    private function callNextElement($after_move)
    {
        if ($this->valid())
        {
            if ($after_move)
            {
                if(($this->mode == self::SELF_FIRST && $this->callHasChildren() || $this->mode == self::LEAVES_ONLY)) {
                  $this->nextElement();
                }
            }
            else
            {
                $this->nextElement();
            }
        }
    }
    
    function nextElement() {
    
    }

    function setMaxDepth($max_depth = -1)
    {
        $max_depth = (int)$max_depth;
        if ($max_depth < -1) {
            throw new OutOfRangeException('Parameter max_depth must be >= -1');
        }
        $this->max_depth = $max_depth;
    }
    
    function getMaxDepth()
    {
        return $this->max_depth == -1 ? false : $this->max_depth;
    }
}

?>