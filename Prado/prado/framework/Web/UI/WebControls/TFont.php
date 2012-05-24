<?php
/**
 * TFont class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TFont.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TFont class
 *
 * TFont encapsulates the CSS style fields related with font settings.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TFont.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TFont extends TComponent
{
	/**
	 * Bits indicating the font states.
	 */
	const IS_BOLD=0x01;
	const IS_ITALIC=0x02;
	const IS_OVERLINE=0x04;
	const IS_STRIKEOUT=0x08;
	const IS_UNDERLINE=0x10;

	/**
	 * Bits indicating whether particular font states are changed.
	 */
	const IS_SET_BOLD=0x01000;
	const IS_SET_ITALIC=0x02000;
	const IS_SET_OVERLINE=0x04000;
	const IS_SET_STRIKEOUT=0x08000;
	const IS_SET_UNDERLINE=0x10000;
	const IS_SET_SIZE=0x20000;
	const IS_SET_NAME=0x40000;

	/**
	 * @var integer bits representing various states
	 */
	private $_flags=0;
	/**
	 * @var string font name
	 */
	private $_name='';
	/**
	 * @var string font size
	 */
	private $_size='';

	/**
	 * @return boolean whether the font is in bold face. Defaults to false.
	 */
	public function getBold()
	{
		return ($this->_flags & self::IS_BOLD)!==0;
	}

	/**
	 * @param boolean whether the font is in bold face
	 */
	public function setBold($value)
	{
		$this->_flags |= self::IS_SET_BOLD;
		if(TPropertyValue::ensureBoolean($value))
			$this->_flags |= self::IS_BOLD;
		else
			$this->_flags &= ~self::IS_BOLD;
	}

	/**
	 * @return boolean whether the font is in italic face. Defaults to false.
	 */
	public function getItalic()
	{
		return ($this->_flags & self::IS_ITALIC)!==0;
	}

	/**
	 * @param boolean whether the font is italic
	 */
	public function setItalic($value)
	{
		$this->_flags |= self::IS_SET_ITALIC;
		if(TPropertyValue::ensureBoolean($value))
			$this->_flags |= self::IS_ITALIC;
		else
			$this->_flags &= ~self::IS_ITALIC;
	}

	/**
	 * @return boolean whether the font is overlined. Defaults to false.
	 */
	public function getOverline()
	{
		return ($this->_flags & self::IS_OVERLINE)!==0;
	}

	/**
	 * @param boolean whether the font is overlined
	 */
	public function setOverline($value)
	{
		$this->_flags |= self::IS_SET_OVERLINE;
		if(TPropertyValue::ensureBoolean($value))
			$this->_flags |= self::IS_OVERLINE;
		else
			$this->_flags &= ~self::IS_OVERLINE;
	}

	/**
	 * @return string the font size
	 */
	public function getSize()
	{
		return $this->_size;
	}

	/**
	 * @param string the font size
	 */
	public function setSize($value)
	{
		$this->_flags |= self::IS_SET_SIZE;
		$this->_size=$value;
	}

	/**
	 * @return boolean whether the font is strikeout. Defaults to false.
	 */
	public function getStrikeout()
	{
		return ($this->_flags & self::IS_STRIKEOUT)!==0;
	}

	/**
	 * @param boolean whether the font is strikeout
	 */
	public function setStrikeout($value)
	{
		$this->_flags |= self::IS_SET_STRIKEOUT;
		if(TPropertyValue::ensureBoolean($value))
			$this->_flags |= self::IS_STRIKEOUT;
		else
			$this->_flags &= ~self::IS_STRIKEOUT;
	}

	/**
	 * @return boolean whether the font is underlined. Defaults to false.
	 */
	public function getUnderline()
	{
		return ($this->_flags & self::IS_UNDERLINE)!==0;
	}

	/**
	 * @param boolean whether the font is underlined
	 */
	public function setUnderline($value)
	{
		$this->_flags |= self::IS_SET_UNDERLINE;
		if(TPropertyValue::ensureBoolean($value))
			$this->_flags |= self::IS_UNDERLINE;
		else
			$this->_flags &= ~self::IS_UNDERLINE;
	}

	/**
	 * @return string the font name (family)
	 */
	public function getName()
	{
		return $this->_name;
	}

	/**
	 * @param string the font name (family)
	 */
	public function setName($value)
	{
		$this->_flags |= self::IS_SET_NAME;
		$this->_name=$value;
	}

	/**
	 * @return boolean whether the font is empty
	 */
	public function getIsEmpty()
	{
		return !$this->_flags;
	}

	/**
	 * Clears up the font.
	 */
	public function reset()
	{
		$this->_flags=0;
		$this->_name='';
		$this->_size='';
	}

	/**
	 * Merges the font with a new one.
	 * If a font field is not set in the font, it will be overwritten with
	 * the new one.
	 * @param TFont the new font
	 */
	public function mergeWith($font)
	{
		if($font===null || $font->_flags===0)
			return;
		if(!($this->_flags & self::IS_SET_BOLD) && ($font->_flags & self::IS_SET_BOLD))
			$this->setBold($font->getBold());
		if(!($this->_flags & self::IS_SET_ITALIC) && ($font->_flags & self::IS_SET_ITALIC))
			$this->setItalic($font->getItalic());
		if(!($this->_flags & self::IS_SET_OVERLINE) && ($font->_flags & self::IS_SET_OVERLINE))
			$this->setOverline($font->getOverline());
		if(!($this->_flags & self::IS_SET_STRIKEOUT) && ($font->_flags & self::IS_SET_STRIKEOUT))
			$this->setStrikeout($font->getStrikeout());
		if(!($this->_flags & self::IS_SET_UNDERLINE) && ($font->_flags & self::IS_SET_UNDERLINE))
			$this->setUnderline($font->getUnderline());
		if(!($this->_flags & self::IS_SET_SIZE) && ($font->_flags & self::IS_SET_SIZE))
			$this->setSize($font->getSize());
		if(!($this->_flags & self::IS_SET_NAME) && ($font->_flags & self::IS_SET_NAME))
			$this->setName($font->getName());
	}

	/**
	 * Copies the fields in a new font to this font.
	 * If a font field is set in the new font, the corresponding field
	 * in this font will be overwritten.
	 * @param TFont the new font
	 */
	public function copyFrom($font)
	{
		if($font===null || $font->_flags===0)
			return;
		if($font->_flags & self::IS_SET_BOLD)
			$this->setBold($font->getBold());
		if($font->_flags & self::IS_SET_ITALIC)
			$this->setItalic($font->getItalic());
		if($font->_flags & self::IS_SET_OVERLINE)
			$this->setOverline($font->getOverline());
		if($font->_flags & self::IS_SET_STRIKEOUT)
			$this->setStrikeout($font->getStrikeout());
		if($font->_flags & self::IS_SET_UNDERLINE)
			$this->setUnderline($font->getUnderline());
		if($font->_flags & self::IS_SET_SIZE)
			$this->setSize($font->getSize());
		if($font->_flags & self::IS_SET_NAME)
			$this->setName($font->getName());
	}

	/**
	 * @return string the font in a css style string representation.
	 */
	public function toString()
	{
		if($this->_flags===0)
			return '';
		$str='';
		if($this->_flags & self::IS_SET_BOLD)
			$str.='font-weight:'.(($this->_flags & self::IS_BOLD)?'bold;':'normal;');
		if($this->_flags & self::IS_SET_ITALIC)
			$str.='font-style:'.(($this->_flags & self::IS_ITALIC)?'italic;':'normal;');
		$textDec='';
		if($this->_flags & self::IS_UNDERLINE)
			$textDec.='underline';
		if($this->_flags & self::IS_OVERLINE)
			$textDec.=' overline';
		if($this->_flags & self::IS_STRIKEOUT)
			$textDec.=' line-through';
		$textDec=ltrim($textDec);
		if($textDec!=='')
			$str.='text-decoration:'.$textDec.';';
		if($this->_size!=='')
			$str.='font-size:'.$this->_size.';';
		if($this->_name!=='')
			$str.='font-family:'.$this->_name.';';
		return $str;
	}

	/**
	 * Adds attributes related to CSS styles to renderer.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function addAttributesToRender($writer)
	{
		if($this->_flags===0)
			return;
		if($this->_flags & self::IS_SET_BOLD)
			$writer->addStyleAttribute('font-weight',(($this->_flags & self::IS_BOLD)?'bold':'normal'));
		if($this->_flags & self::IS_SET_ITALIC)
			$writer->addStyleAttribute('font-style',(($this->_flags & self::IS_ITALIC)?'italic':'normal'));
		$textDec='';
		if($this->_flags & self::IS_UNDERLINE)
			$textDec.='underline';
		if($this->_flags & self::IS_OVERLINE)
			$textDec.=' overline';
		if($this->_flags & self::IS_STRIKEOUT)
			$textDec.=' line-through';
		$textDec=ltrim($textDec);
		if($textDec!=='')
			$writer->addStyleAttribute('text-decoration',$textDec);
		if($this->_size!=='')
			$writer->addStyleAttribute('font-size',$this->_size);
		if($this->_name!=='')
			$writer->addStyleAttribute('font-family',$this->_name);
	}
}
