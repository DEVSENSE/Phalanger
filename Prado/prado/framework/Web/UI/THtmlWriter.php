<?php
/**
 * THtmlWriter class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: THtmlWriter.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 */

/**
 * THtmlWriter class
 *
 * THtmlWriter is a writer that renders valid XHTML outputs.
 * It provides functions to render tags, their attributes and stylesheet fields.
 * Attribute and stylesheet values will be automatically HTML-encoded if
 * they require so. For example, the 'value' attribute in an input tag
 * will be encoded.
 *
 * A common usage of THtmlWriter is as the following sequence:
 * <code>
 *  $writer->addAttribute($name1,$value1);
 *  $writer->addAttribute($name2,$value2);
 *  $writer->renderBeginTag($tagName);
 *  // ... render contents enclosed within the tag here
 *  $writer->renderEndTag();
 * </code>
 * Make sure each invocation of {@link renderBeginTag} is accompanied with
 * a {@link renderEndTag} and they are properly nested, like nesting
 * tags in HTML and XHTML.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THtmlWriter.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 * @since 3.0
 */
class THtmlWriter extends TApplicationComponent implements ITextWriter
{
	/**
	 * @var array list of tags are do not need a closing tag
	 */
	private static $_simpleTags=array(
		'area'=>true,
		'base'=>true,
		'basefont'=>true,
		'bgsound'=>true,
		'col'=>true,
		'embed'=>true,
		'frame'=>true,
		'hr'=>true,
		'img'=>true,
		'input'=>true,
		'isindex'=>true,
		'link'=>true,
		'meta'=>true,
		'wbr'=>true,
	);
	/**
	 * @var array list of attributes that need HTML encoding
	 */
	private static $_attrEncode=array(
		'abbr'=>true,
		'accesskey'=>true,
		'alt'=>true,
		'axis'=>true,
		'background'=>true,
		'class'=>true,
		'content'=>true,
		'headers'=>true,
		'href'=>true,
		'longdesc'=>true,
		'onclick'=>true,
		'onchange'=>true,
		'src'=>true,
		'title'=>true,
		'label'=>true,
		'value'=>true
	);
	/**
	 * @var array list of stylesheet attributes that need HTML encoding
	 */
	private static $_styleEncode=array(
		'background-image'=>true,
		'list-style-image'=>true
	);
	/**
	 * @var array list of attributes to be rendered for a tag
	 */
	private $_attributes=array();
	/**
	 * @var array list of openning tags
	 */
	private $_openTags=array();
	/**
	 * @var array list of style attributes
	 */
	private $_styles=array();
	/**
	 * @var ITextWriter writer
	 */
	private $_writer=null;

	/**
	 * Constructor.
	 * @param ITextWriter a writer that THtmlWriter will pass its rendering result to
	 */
	public function __construct($writer)
	{
		$this->_writer=$writer;
	}

	public function getWriter()
	{
		return $this->_writer;
	}
	
	public function setWriter($writer)
	{
		$this->_writer = $writer;
	}
	/**
	 * Adds a list of attributes to be rendered.
	 * @param array list of attributes to be rendered
	 */
	public function addAttributes($attrs)
	{
		foreach($attrs as $name=>$value)
			$this->_attributes[$name]=isset(self::$_attrEncode[$name])?THttpUtility::htmlEncode($value):$value;
	}

	/**
	 * Adds an attribute to be rendered.
	 * @param string name of the attribute
	 * @param string value of the attribute
	 */
	public function addAttribute($name,$value)
	{
		$this->_attributes[$name]=isset(self::$_attrEncode[$name])?THttpUtility::htmlEncode($value):$value;
	}

	/**
	 * Removes the named attribute from rendering
	 * @param string name of the attribute to be removed
	 */
	public function removeAttribute($name)
	{
		unset($this->_attributes[$name]);
	}

	/**
	 * Adds a list of stylesheet attributes to be rendered.
	 * @param array list of stylesheet attributes to be rendered
	 */
	public function addStyleAttributes($attrs)
	{
		foreach($attrs as $name=>$value)
			$this->_styles[$name]=isset(self::$_styleEncode[$name])?THttpUtility::htmlEncode($value):$value;
	}

	/**
	 * Adds a stylesheet attribute to be rendered
	 * @param string stylesheet attribute name
	 * @param string stylesheet attribute value
	 */
	public function addStyleAttribute($name,$value)
	{
		$this->_styles[$name]=isset(self::$_styleEncode[$name])?THttpUtility::htmlEncode($value):$value;
	}

	/**
	 * Removes the named stylesheet attribute from rendering
	 * @param string name of the stylesheet attribute to be removed
	 */
	public function removeStyleAttribute($name)
	{
		unset($this->_styles[$name]);
	}

	/**
	 * Flushes the rendering result.
	 * This will invoke the underlying writer's flush method.
	 */
	public function flush()
	{
		return $this->_writer->flush();
	}

	/**
	 * Renders a string.
	 * @param string string to be rendered
	 */
	public function write($str)
	{
		$this->_writer->write($str);
	}

	/**
	 * Renders a string and appends a newline to it.
	 * @param string string to be rendered
	 */
	public function writeLine($str='')
	{
		$this->_writer->write($str."\n");
	}

	/**
	 * Renders an HTML break.
	 */
	public function writeBreak()
	{
		$this->_writer->write('<br/>');
	}

	/**
	 * Renders the openning tag.
	 * @param string tag name
	 */
	public function renderBeginTag($tagName)
	{
		$str='<'.$tagName;
		foreach($this->_attributes as $name=>$value)
			$str.=' '.$name.'="'.$value.'"';
		if(!empty($this->_styles))
		{
			$str.=' style="';
			foreach($this->_styles as $name=>$value)
				$str.=$name.':'.$value.';';
			$str.='"';
		}
		if(isset(self::$_simpleTags[$tagName]))
		{
			$str.=' />';
			$this->_openTags[] = '';
		}
		else
		{
			$str.='>';
			$this->_openTags[] = $tagName;
		}
		$this->_writer->write($str);
		$this->_attributes=array();
		$this->_styles=array();
	}

	/**
	 * Renders the closing tag.
	 */
	public function renderEndTag()
	{
		if(!empty($this->_openTags) && ($tagName=array_pop($this->_openTags))!=='')
			$this->_writer->write('</'.$tagName.'>');
	}
}

