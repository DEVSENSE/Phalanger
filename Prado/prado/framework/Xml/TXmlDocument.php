<?php
/**
 * TXmlElement, TXmlDocument, TXmlElementList class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TXmlDocument.php 2771 2010-02-16 14:55:38Z Christophe.Boulain $
 * @package System.Xml
 */

/**
 * TXmlElement class.
 *
 * TXmlElement represents an XML element node.
 * You can obtain its tag-name, attributes, text between the opening and closing
 * tags via the TagName, Attributes, and Value properties, respectively.
 * You can also retrieve its parent and child elements by Parent and Elements
 * properties, respectively.
 *
 * TBD: xpath
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TXmlDocument.php 2771 2010-02-16 14:55:38Z Christophe.Boulain $
 * @package System.Xml
 * @since 3.0
 */
class TXmlElement extends TComponent
{
	/**
	 * @var TXmlElement parent of this element
	 */
	private $_parent=null;
	/**
	 * @var string tag-name of this element
	 */
	private $_tagName='unknown';
	/**
	 * @var string text enclosed between opening and closing tags of this element
	 */
	private $_value='';
	/**
	 * @var TXmlElementList list of child elements of this element
	 */
	private $_elements=null;
	/**
	 * @var TMap attributes of this element
	 */
	private $_attributes=null;

	/**
	 * Constructor.
	 * @param string tag-name for this element
	 */
	public function __construct($tagName)
	{
		$this->setTagName($tagName);
	}

	/**
	 * @return TXmlElement parent element of this element
	 */
	public function getParent()
	{
		return $this->_parent;
	}

	/**
	 * @param TXmlElement parent element of this element
	 */
	public function setParent($parent)
	{
		$this->_parent=$parent;
	}

	/**
	 * @return string tag-name of this element
	 */
	public function getTagName()
	{
		return $this->_tagName;
	}

	/**
	 * @param string tag-name of this element
	 */
	public function setTagName($tagName)
	{
		$this->_tagName=$tagName;
	}

	/**
	 * @return string text enclosed between opening and closing tag of this element
	 */
	public function getValue()
	{
		return $this->_value;
	}

	/**
	 * @param string text enclosed between opening and closing tag of this element
	 */
	public function setValue($value)
	{
		$this->_value=TPropertyValue::ensureString($value);
	}

	/**
	 * @return boolean true if this element has child elements
	 */
	public function getHasElement()
	{
		return $this->_elements!==null && $this->_elements->getCount()>0;
	}

	/**
	 * @return boolean true if this element has attributes
	 */
	public function getHasAttribute()
	{
		return $this->_attributes!==null && $this->_attributes->getCount()>0;
	}

	/**
	 * @return string the attribute specified by the name, null if no such attribute
	 */
	public function getAttribute($name)
	{
		if($this->_attributes!==null)
			return $this->_attributes->itemAt($name);
		else
			return null;
	}

	/**
	 * @param string attribute name
	 * @param string attribute value
	 */
	public function setAttribute($name,$value)
	{
		$this->getAttributes()->add($name,TPropertyValue::ensureString($value));
	}

	/**
	 * @return TXmlElementList list of child elements
	 */
	public function getElements()
	{
		if(!$this->_elements)
			$this->_elements=new TXmlElementList($this);
		return $this->_elements;
	}

	/**
	 * @return TMap list of attributes
	 */
	public function getAttributes()
	{
		if(!$this->_attributes)
			$this->_attributes=new TMap;
		return $this->_attributes;
	}

	/**
	 * @return TXmlElement the first child element that has the specified tag-name, null if not found
	 */
	public function getElementByTagName($tagName)
	{
		if($this->_elements)
		{
			foreach($this->_elements as $element)
				if($element->_tagName===$tagName)
					return $element;
		}
		return null;
	}

	/**
	 * @return TList list of all child elements that have the specified tag-name
	 */
	public function getElementsByTagName($tagName)
	{
		$list=new TList;
		if($this->_elements)
		{
			foreach($this->_elements as $element)
				if($element->_tagName===$tagName)
					$list->add($element);
		}
		return $list;
	}

	/**
	 * @return string string representation of this element
	 */
	public function toString($indent=0)
	{
		$attr='';
		if($this->_attributes!==null)
		{
			foreach($this->_attributes as $name=>$value)
			{
				$value=$this->xmlEncode($value);
				$attr.=" $name=\"$value\"";
			}
		}
		$prefix=str_repeat(' ',$indent*4);
		if($this->getHasElement())
		{
			$str=$prefix."<{$this->_tagName}$attr>\n";
			foreach($this->getElements() as $element)
				$str.=$element->toString($indent+1)."\n";
			$str.=$prefix."</{$this->_tagName}>";
			return $str;
		}
		else if(($value=$this->getValue())!=='')
		{
			$value=$this->xmlEncode($value);
			return $prefix."<{$this->_tagName}$attr>$value</{$this->_tagName}>";
		}
		else
			return $prefix."<{$this->_tagName}$attr />";
	}

	/**
	 * Magic-method override. Called whenever this element is used as a string.
	 * <code>
	 * $element = new TXmlElement('tag');
	 * echo $element;
	 * </code>
	 * or
	 * <code>
	 * $element = new TXmlElement('tag');
	 * $xml = (string)$element;
	 * </code>
	 * @return string string representation of this element
	 */
	public function __toString()
	{
		return $this->toString();
	}

	private function xmlEncode($str)
	{
		return strtr($str,array(
			'>'=>'&gt;',
			'<'=>'&lt;',
			'&'=>'&amp;',
			'"'=>'&quot;',
			"\r"=>'&#xD;',
			"\t"=>'&#x9;',
			"\n"=>'&#xA;'));
	}
}

/**
 * TXmlDocument class.
 *
 * TXmlDocument represents a DOM representation of an XML file.
 * Besides all properties and methods inherited from {@link TXmlElement},
 * you can load an XML file or string by {@link loadFromFile} or {@link loadFromString}.
 * You can also get the version and encoding of the XML document by
 * the Version and Encoding properties.
 *
 * To construct an XML string, you may do the following:
 * <code>
 * $doc=new TXmlDocument('1.0','utf-8');
 * $doc->TagName='Root';
 *
 * $proc=new TXmlElement('Proc');
 * $proc->setAttribute('Name','xxxx');
 * $doc->Elements[]=$proc;
 *
 * $query=new TXmlElement('Query');
 * $query->setAttribute('ID','xxxx');
 * $proc->Elements[]=$query;
 *
 * $attr=new TXmlElement('Attr');
 * $attr->setAttribute('Name','aaa');
 * $attr->Value='1';
 * $query->Elements[]=$attr;
 *
 * $attr=new TXmlElement('Attr');
 * $attr->setAttribute('Name','bbb');
 * $attr->Value='1';
 * $query->Elements[]=$attr;
 * </code>
 * The above code represents the following XML string:
 * <code>
 * <?xml version="1.0" encoding="utf-8"?>
 * <Root>
 *   <Proc Name="xxxx">
 *     <Query ID="xxxx">
 *       <Attr Name="aaa">1</Attr>
 *       <Attr Name="bbb">1</Attr>
 *     </Query>
 *   </Proc>
 * </Root>
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TXmlDocument.php 2771 2010-02-16 14:55:38Z Christophe.Boulain $
 * @package System.Xml
 * @since 3.0
 */
class TXmlDocument extends TXmlElement
{
	/**
	 * @var string version of this XML document
	 */
	private $_version;
	/**
	 * @var string encoding of this XML document
	 */
	private $_encoding;

	/**
	 * Constructor.
	 * @param string version of this XML document
	 * @param string encoding of this XML document
	 */
	public function __construct($version='1.0',$encoding='')
	{
		parent::__construct('');
		$this->setVersion($version);
		$this->setEncoding($encoding);
	}

	/**
	 * @return string version of this XML document
	 */
	public function getVersion()
	{
		return $this->_version;
	}

	/**
	 * @param string version of this XML document
	 */
	public function setVersion($version)
	{
		$this->_version=$version;
	}

	/**
	 * @return string encoding of this XML document
	 */
	public function getEncoding()
	{
		return $this->_encoding;
	}

	/**
	 * @param string encoding of this XML document
	 */
	public function setEncoding($encoding)
	{
		$this->_encoding=$encoding;
	}

	/**
	 * Loads and parses an XML document.
	 * @param string the XML file path
	 * @return boolean whether the XML file is parsed successfully
	 * @throws TIOException if the file fails to be opened.
	 */
	public function loadFromFile($file)
	{
		if(($str=@file_get_contents($file))!==false)
			return $this->loadFromString($str);
		else
			throw new TIOException('xmldocument_file_read_failed',$file);
	}

	/**
	 * Loads and parses an XML string.
	 * The version and encoding will be determined based on the parsing result.
	 * @param string the XML string
	 * @return boolean whether the XML string is parsed successfully
	 */
	public function loadFromString($string)
	{
		// TODO: since PHP 5.1, we can get parsing errors and throw them as exception
		$doc=new DOMDocument();
		if($doc->loadXML($string)===false)
			return false;

		$this->setEncoding($doc->encoding);
		$this->setVersion($doc->version);

		$element=$doc->documentElement;
		$this->setTagName($element->tagName);
		$this->setValue($element->nodeValue);
		$elements=$this->getElements();
		$attributes=$this->getAttributes();
		$elements->clear();
		$attributes->clear();

		static $bSimpleXml;
		if($bSimpleXml === null)
			$bSimpleXml = (boolean)function_exists('simplexml_load_string');

		if($bSimpleXml)
		{
			$simpleDoc = simplexml_load_string($string);
			$docNamespaces = $simpleDoc->getDocNamespaces(false);
			$simpleDoc = null;
			foreach($docNamespaces as $prefix => $uri)
			{
 				if($prefix === '')
   					$attributes->add('xmlns', $uri);
   				else
   					$attributes->add('xmlns:'.$prefix, $uri);
			}
		}

		foreach($element->attributes as $name=>$attr)
			$attributes->add(($attr->prefix === '' ? '' : $attr->prefix . ':') .$name,$attr->value);
		foreach($element->childNodes as $child)
		{
			if($child instanceof DOMElement)
				$elements->add($this->buildElement($child));
		}

		return true;
	}

	/**
	 * Saves this XML document as an XML file.
	 * @param string the name of the file to be stored with XML output
	 * @throws TIOException if the file cannot be written
	 */
	public function saveToFile($file)
	{
		if(($fw=fopen($file,'w'))!==false)
		{
			fwrite($fw,$this->saveToString());
			fclose($fw);
		}
		else
			throw new TIOException('xmldocument_file_write_failed',$file);
	}

	/**
	 * Saves this XML document as an XML string
	 * @return string the XML string of this XML document
	 */
	public function saveToString()
	{
		$version=empty($this->_version)?' version="1.0"':' version="'.$this->_version.'"';
		$encoding=empty($this->_encoding)?'':' encoding="'.$this->_encoding.'"';
		return "<?xml{$version}{$encoding}?>\n".$this->toString(0);
	}

	/**
	 * Magic-method override. Called whenever this document is used as a string.
	 * <code>
	 * $document = new TXmlDocument();
	 * $document->TagName = 'root';
	 * echo $document;
	 * </code>
	 * or
	 * <code>
	 * $document = new TXmlDocument();
	 * $document->TagName = 'root';
	 * $xml = (string)$document;
	 * </code>
	 * @return string string representation of this document
	 */
	public function __toString()
	{
		return $this->saveToString();
	}

	/**
	 * Recursively converts DOM XML nodes into TXmlElement
	 * @param DOMXmlNode the node to be converted
	 * @return TXmlElement the converted TXmlElement
	 */
	private function buildElement($node)
	{
		$element=new TXmlElement($node->tagName);
		$element->setValue($node->nodeValue);
		foreach($node->attributes as $name=>$attr)
			$element->getAttributes()->add(($attr->prefix === '' ? '' : $attr->prefix . ':') . $name,$attr->value);

		foreach($node->childNodes as $child)
		{
			if($child instanceof DOMElement)
				$element->getElements()->add($this->buildElement($child));
		}
		return $element;
	}
}


/**
 * TXmlElementList class.
 *
 * TXmlElementList represents a collection of {@link TXmlElement}.
 * You may manipulate the collection with the operations defined in {@link TList}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TXmlDocument.php 2771 2010-02-16 14:55:38Z Christophe.Boulain $
 * @package System.Xml
 * @since 3.0
 */
class TXmlElementList extends TList
{
	/**
	 * @var TXmlElement owner of this list
	 */
	private $_o;

	/**
	 * Constructor.
	 * @param TXmlElement owner of this list
	 */
	public function __construct(TXmlElement $owner)
	{
		$this->_o=$owner;
	}

	/**
	 * @return TXmlElement owner of this list
	 */
	protected function getOwner()
	{
		return $this->_o;
	}

	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by performing additional
	 * operations for each newly added TXmlElement object.
	 * @param integer the specified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a TXmlElement object.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TXmlElement)
		{
			parent::insertAt($index,$item);
			if($item->getParent()!==null)
				$item->getParent()->getElements()->remove($item);
			$item->setParent($this->_o);
		}
		else
			throw new TInvalidDataTypeException('xmlelementlist_xmlelement_required');
	}

	/**
	 * Removes an item at the specified position.
	 * This overrides the parent implementation by performing additional
	 * cleanup work when removing a TXmlElement object.
	 * @param integer the index of the item to be removed.
	 * @return mixed the removed item.
	 */
	public function removeAt($index)
	{
		$item=parent::removeAt($index);
		if($item instanceof TXmlElement)
			$item->setParent(null);
		return $item;
	}
}

