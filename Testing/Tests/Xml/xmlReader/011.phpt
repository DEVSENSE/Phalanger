--TEST--
XMLReader: libxml2 XML Reader, string data 
--SKIPIF--
<?php if (!extension_loaded("xmlreader")) print "skip - xmlreader extension not loaded."; ?>
$reader = new XMLReader();
if (!method_exists($reader, 'readInnerXml')) print "skip - readInnerXml method doesn't exist in XMLReader.";
?>
--FILE--
<?php 
/* $Id$ */

$xmlstring = '<?xml version="1.0" encoding="UTF-8"?>
<books><book>test</book></books>';

$reader = new XMLReader();
$reader->XML($xmlstring);
$reader->read();
echo $reader->readInnerXml();
echo "\n";
$reader->close();


$reader = new XMLReader();
$reader->XML($xmlstring);
$reader->read();
echo $reader->readOuterXml();
echo "\n";
$reader->close();
?>
===DONE===
--EXPECT EXACT--
<book>test</book>
<books><book>test</book></books>
===DONE===
