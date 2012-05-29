<?php
define("TEST_CLASS", "TestClass");

if(!defined("TEST_CLASS"))
{
    die("Gna !");
}

class TestClass
{
    const test_const = 1;

    static $test_static_prop = 2;

    function test_method($arg)
    {
    }
}

?>