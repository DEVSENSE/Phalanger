--TEST--
XMLReader: libxml2 XML Reader, initialization
--SKIPIF--
<?php if (!extension_loaded("xmlreader")) print "skip - xmlreader extension not loaded."; ?>
--FILE--
<?php 
/* $Id$ */

$xmlstring = '<?xml version="1.0" encoding="UTF-8"?>
<books><book num="1" idx="2">book1</book></books>';

$reader = new XMLReader();
$reader->XML($xmlstring);
echo $reader->name."\n";
echo $reader->nodeType."\n";
while ($reader->read()) {
	echo $reader->name."\n";
}

$reader->XML($xmlstring);
$reader->read();
echo $reader->nodeType."\n";
echo $reader->name."\n";

$reader->XML($xmlstring);
$reader->read();
$reader->read();
echo $reader->nodeType."\n";
echo $reader->name."\n";

$filename = dirname(__FILE__) . '/_002.xml';
file_put_contents($filename, $xmlstring);
$reader = new XMLReader();
if (!$reader->open($filename)) {
	exit();
}
$reader->read();
echo $reader->name."\n";

$reader->close();
unlink($filename);

?>
===DONE===
--EXPECT EXACT--

0
books
book
#text
book
books
1
books
1
book
books
===DONE===
