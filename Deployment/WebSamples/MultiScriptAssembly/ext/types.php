<?php

class X
{
    var $a, $b;
    
    function X($a, $b)
    {
        $this->a = $a;
        $this->b = $b;
    }
    
    function __toString()
    {
        return "X(){ a = $this->a, b = $this->b };";
    }
}

?>