<?php
session_start();

require_once("reflection.inc.php");

$r = new ReflectionClass('TestClass');

var_dump($r);
echo "<br />";

echo "name : ";
flush();
echo $r->name;
echo "<br />";

echo "getFileName() : ";
flush();
echo $r->getFileName();
echo "<br />";

echo "hasMethod() : ";
flush();
echo $r->hasMethod("test_method");
echo "<br />";

echo "hasConstant() : ";
flush();
echo $r->hasConstant("test_const");
echo "<br />";

echo "getStaticPropertyValue() : ";
flush;
echo $r->getStaticPropertyValue("test_static_prop");
echo "<br />Expected : ";
echo TestClass::$test_static_prop;
echo "<br />";

?>