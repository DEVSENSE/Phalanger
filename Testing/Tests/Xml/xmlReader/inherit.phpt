--TEST--
XMLReader: class inheritance
--SKIPIF--
<?php if (!extension_loaded("xmlreader")) print "skip - xmlreader extension not loaded."; ?>
--FILE--
<?php 
class XMLReader2 extends XMLReader {

	/**
	 * @return bool|string
	 */
	function nodeContents() {
		if( $this->isEmptyElement ) {
			return "";
		}
		$buffer = "";
		while( $this->read() ) {
			switch( $this->nodeType ) {
			case TEXT:
			case XMLReader::SIGNIFICANT_WHITESPACE:
				$buffer .= $this->value;
				break;
			case XmlReader::END_ELEMENT:
				return $buffer;
			}
		}
		return $this->close();
	}
}
?>
===DONE===
--EXPECTF--

===DONE===
