--TEST--
XMLReader: libxml2 XML Reader, string data 
--SKIPIF--
<?php if (!extension_loaded("xmlreader")) print "skip - xmlreader extension not loaded."; ?>
--FILE--
<?php 
/* $Id$ */

$xmlstring = '<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<LIST>
<MOVIE ID="x200338360">
<TITLE>Move Title 1</TITLE>
<ORGTITLE/><LOC>Location 1</LOC>
<INFO/>
</MOVIE>
<MOVIE ID="m200338361">
<TITLE>Move Title 2</TITLE>
<ORGTITLE/>
<LOC>Location 2</LOC>
<INFO/>
</MOVIE>
</LIST>';

$reader = new XMLReader();
$reader->XML($xmlstring);

// Only go through
while ($reader->read()) {
	echo $reader->name."\n";
}
?>
===DONE===
--EXPECT EXACT--
LIST
LIST
#text
MOVIE
#text
TITLE
#text
TITLE
#text
ORGTITLE
LOC
#text
LOC
#text
INFO
#text
MOVIE
#text
MOVIE
#text
TITLE
#text
TITLE
#text
ORGTITLE
#text
LOC
#text
LOC
#text
INFO
#text
MOVIE
#text
LIST
===DONE===
