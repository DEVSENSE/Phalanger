<?php
/**
 * TStyle class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TFont definition
 */
Prado::using('System.Web.UI.WebControls.TFont');

/**
 * TStyle class
 *
 * TStyle encapsulates the CSS style applied to a control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TStyle extends TComponent
{
	/**
	 * @var array storage of CSS fields
	 */
	private $_fields=array();
	/**
	 * @var TFont font object
	 */
	private $_font=null;
	/**
	 * @var string CSS class name
	 */
	private $_class=null;
	/**
	 * @var string CSS style string (those not represented by specific fields of TStyle)
	 */
	private $_customStyle=null;
	/**
	 * @var string display style
	 */
	private $_displayStyle='Fixed';

	/**
	 * Constructor.
	 * @param TStyle style to copy from
	 */
	public function __construct($style=null)
	{
		if($style!==null)
			$this->copyFrom($style);
	}

	/**
	 * Need to clone the font object.
	 */
	public function __clone()
	{
		if($this->_font!==null)
			$this->_font = clone($this->_font);
	}

	/**
	 * @return string the background color of the control
	 */
	public function getBackColor()
	{
		return isset($this->_fields['background-color'])?$this->_fields['background-color']:'';
	}

	/**
	 * @param string the background color of the control
	 */
	public function setBackColor($value)
	{
		if(trim($value)==='')
			unset($this->_fields['background-color']);
		else
			$this->_fields['background-color']=$value;
	}

	/**
	 * @return string the border color of the control
	 */
	public function getBorderColor()
	{
		return isset($this->_fields['border-color'])?$this->_fields['border-color']:'';
	}

	/**
	 * @param string the border color of the control
	 */
	public function setBorderColor($value)
	{
		if(trim($value)==='')
			unset($this->_fields['border-color']);
		else
			$this->_fields['border-color']=$value;
	}

	/**
	 * @return string the border style of the control
	 */
	public function getBorderStyle()
	{
		return isset($this->_fields['border-style'])?$this->_fields['border-style']:'';
	}

	/**
	 * Sets the border style of the control.
	 * @param string the border style of the control
	 */
	public function setBorderStyle($value)
	{
		if(trim($value)==='')
			unset($this->_fields['border-style']);
		else
			$this->_fields['border-style']=$value;
	}

	/**
	 * @return string the border width of the control
	 */
	public function getBorderWidth()
	{
		return isset($this->_fields['border-width'])?$this->_fields['border-width']:'';
	}

	/**
	 * @param string the border width of the control
	 */
	public function setBorderWidth($value)
	{
		if(trim($value)==='')
			unset($this->_fields['border-width']);
		else
			$this->_fields['border-width']=$value;
	}

	/**
	 * @return string the CSS class of the control
	 */
	public function getCssClass()
	{
		return $this->_class===null?'':$this->_class;
	}

	/**
	 * @return boolean true if CSS is set or empty.
	 */
	public function hasCssClass()
	{
		return ($this->_class!==null);
	}

	/**
	 * @param string the name of the CSS class of the control
	 */
	public function setCssClass($value)
	{
		$this->_class=$value;
	}

	/**
	 * @return TFont the font of the control
	 */
	public function getFont()
	{
		if($this->_font===null)
			$this->_font=new TFont;
		return $this->_font;
	}

	/**
	 * @return boolean true if font is set.
	 */
	public function hasFont()
	{
		return $this->_font !== null;
	}

	/**
	 * @param TDisplayStyle control display style, default is TDisplayStyle::Fixed
	 */
	public function setDisplayStyle($value)
	{
		$this->_displayStyle = TPropertyValue::ensureEnum($value, 'TDisplayStyle');
		switch($this->_displayStyle)
		{
			case TDisplayStyle::None:
				$this->_fields['display'] = 'none';
				break;
			case TDisplayStyle::Dynamic:
				$this->_fields['display'] = ''; //remove the display property
				break;
			case TDisplayStyle::Fixed:
				$this->_fields['visibility'] = 'visible';
				break;
			case TDisplayStyle::Hidden:
				$this->_fields['visibility'] = 'hidden';
				break;
		}
	}

	/**
	 * @return TDisplayStyle display style
	 */
	public function getDisplayStyle()
	{
		return $this->_displayStyle;
	}

	/**
	 * @return string the foreground color of the control
	 */
	public function getForeColor()
	{
		return isset($this->_fields['color'])?$this->_fields['color']:'';
	}

	/**
	 * @param string the foreground color of the control
	 */
	public function setForeColor($value)
	{
		if(trim($value)==='')
			unset($this->_fields['color']);
		else
			$this->_fields['color']=$value;
	}

	/**
	 * @return string the height of the control
	 */
	public function getHeight()
	{
		return isset($this->_fields['height'])?$this->_fields['height']:'';
	}

	/**
	 * @param string the height of the control
	 */
	public function setHeight($value)
	{
		if(trim($value)==='')
			unset($this->_fields['height']);
		else
			$this->_fields['height']=$value;
	}

	/**
	 * @return string the custom style of the control
	 */
	public function getCustomStyle()
	{
		return $this->_customStyle===null?'':$this->_customStyle;
	}

	/**
	 * Sets custom style fields from a string.
	 * Custom style fields will be overwritten by style fields explicitly defined.
	 * @param string the custom style of the control
	 */
	public function setCustomStyle($value)
	{
		$this->_customStyle=$value;
	}

	/**
	 * @return string a single style field value set via {@link setStyleField}. Defaults to empty string.
	 */
	public function getStyleField($name)
	{
		return isset($this->_fields[$name])?$this->_fields[$name]:'';
	}

	/**
	 * Sets a single style field value.
	 * Style fields set by this method will overwrite those set by {@link setCustomStyle}.
	 * @param string style field name
	 * @param string style field value
	 */
	public function setStyleField($name,$value)
	{
		$this->_fields[$name]=$value;
	}

	/**
	 * Clears a single style field value;
	 * @param string style field name
	 */
	public function clearStyleField($name)
	{
		unset($this->_fields[$name]);
	}

	/**
	 * @return boolean whether a style field has been defined by {@link setStyleField}
	 */
	public function hasStyleField($name)
	{
		return isset($this->_fields[$name]);
	}

	/**
	 * @return string the width of the control
	 */
	public function getWidth()
	{
		return isset($this->_fields['width'])?$this->_fields['width']:'';
	}

	/**
	 * @param string the width of the control
	 */
	public function setWidth($value)
	{
		$this->_fields['width']=$value;
	}

	/**
	 * Resets the style to the original empty state.
	 */
	public function reset()
	{
		$this->_fields=array();
		$this->_font=null;
		$this->_class=null;
		$this->_customStyle=null;
	}

	/**
	 * Copies the fields in a new style to this style.
	 * If a style field is set in the new style, the corresponding field
	 * in this style will be overwritten.
	 * @param TStyle the new style
	 */
	public function copyFrom($style)
	{
		if($style instanceof TStyle)
		{
			$this->_fields=array_merge($this->_fields,$style->_fields);
			if($style->_class!==null)
				$this->_class=$style->_class;
			if($style->_customStyle!==null)
				$this->_customStyle=$style->_customStyle;
			if($style->_font!==null)
				$this->getFont()->copyFrom($style->_font);
		}
	}

	/**
	 * Merges the style with a new one.
	 * If a style field is not set in this style, it will be overwritten by
	 * the new one.
	 * @param TStyle the new style
	 */
	public function mergeWith($style)
	{
		if($style instanceof TStyle)
		{
			$this->_fields=array_merge($style->_fields,$this->_fields);
			if($this->_class===null)
				$this->_class=$style->_class;
			if($this->_customStyle===null)
				$this->_customStyle=$style->_customStyle;
			if($style->_font!==null)
				$this->getFont()->mergeWith($style->_font);
		}
	}

	/**
	 * Adds attributes related to CSS styles to renderer.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function addAttributesToRender($writer)
	{
		if($this->_customStyle!==null)
		{
			foreach(explode(';',$this->_customStyle) as $style)
			{
				$arr=explode(':',$style);
				if(isset($arr[1]) && trim($arr[0])!=='')
					$writer->addStyleAttribute(trim($arr[0]),trim($arr[1]));
			}
		}
		$writer->addStyleAttributes($this->_fields);
		if($this->_font!==null)
			$this->_font->addAttributesToRender($writer);
		if($this->_class!==null)
			$writer->addAttribute('class',$this->_class);
	}

	/**
	 * @return array list of style fields.
	 */
	public function getStyleFields()
	{
		return $this->_fields;
	}
}

/**
 * TDisplayStyle defines the enumerable type for the possible styles
 * that a web control can display.
 *
 * The following enumerable values are defined:
 * - None: the control is not displayed and not included in the layout.
 * - Dynamic: the control is displayed and included in the layout, the layout flow is dependent on the control (equivalent to display:'' in css).
 * - Fixed: Similar to Dynamic with CSS "visibility" set "shown".
 * - Hidden: the control is not displayed and is included in the layout.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.1
 */
class TDisplayStyle extends TEnumerable
{
	const None='None';
	const Dynamic='Dynamic';
	const Fixed='Fixed';
	const Hidden='Hidden';
}

/**
 * TTableStyle class.
 * TTableStyle represents the CSS style specific for HTML table.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTableStyle extends TStyle
{
	/**
	 * @var TVerticalAlign the URL of the background image for the table
	 */
	private $_backImageUrl=null;
	/**
	 * @var THorizontalAlign horizontal alignment of the contents within the table
	 */
	private $_horizontalAlign=null;
	/**
	 * @var integer cellpadding of the table
	 */
	private $_cellPadding=null;
	/**
	 * @var integer cellspacing of the table
	 */
	private $_cellSpacing=null;
	/**
	 * @var TTableGridLines grid line setting of the table
	 */
	private $_gridLines=null;
	/**
	 * @var boolean whether the table border should be collapsed
	 */
	private $_borderCollapse=null;

	/**
	 * Sets the style attributes to default values.
	 * This method overrides the parent implementation by
	 * resetting additional TTableStyle specific attributes.
	 */
	public function reset()
	{
		$this->_backImageUrl=null;
		$this->_horizontalAlign=null;
		$this->_cellPadding=null;
		$this->_cellSpacing=null;
		$this->_gridLines=null;
		$this->_borderCollapse=null;
	}

	/**
	 * Copies the fields in a new style to this style.
	 * If a style field is set in the new style, the corresponding field
	 * in this style will be overwritten.
	 * @param TStyle the new style
	 */
	public function copyFrom($style)
	{
		parent::copyFrom($style);
		if($style instanceof TTableStyle)
		{
			if($style->_backImageUrl!==null)
				$this->_backImageUrl=$style->_backImageUrl;
			if($style->_horizontalAlign!==null)
				$this->_horizontalAlign=$style->_horizontalAlign;
			if($style->_cellPadding!==null)
				$this->_cellPadding=$style->_cellPadding;
			if($style->_cellSpacing!==null)
				$this->_cellSpacing=$style->_cellSpacing;
			if($style->_gridLines!==null)
				$this->_gridLines=$style->_gridLines;
			if($style->_borderCollapse!==null)
				$this->_borderCollapse=$style->_borderCollapse;
		}
	}

	/**
	 * Merges the style with a new one.
	 * If a style field is not set in this style, it will be overwritten by
	 * the new one.
	 * @param TStyle the new style
	 */
	public function mergeWith($style)
	{
		parent::mergeWith($style);
		if($style instanceof TTableStyle)
		{
			if($this->_backImageUrl===null && $style->_backImageUrl!==null)
				$this->_backImageUrl=$style->_backImageUrl;
			if($this->_horizontalAlign===null && $style->_horizontalAlign!==null)
				$this->_horizontalAlign=$style->_horizontalAlign;
			if($this->_cellPadding===null && $style->_cellPadding!==null)
				$this->_cellPadding=$style->_cellPadding;
			if($this->_cellSpacing===null && $style->_cellSpacing!==null)
				$this->_cellSpacing=$style->_cellSpacing;
			if($this->_gridLines===null && $style->_gridLines!==null)
				$this->_gridLines=$style->_gridLines;
			if($this->_borderCollapse===null && $style->_borderCollapse!==null)
				$this->_borderCollapse=$style->_borderCollapse;
		}
	}


	/**
	 * Adds attributes related to CSS styles to renderer.
	 * This method overrides the parent implementation.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function addAttributesToRender($writer)
	{
		if(($url=trim($this->getBackImageUrl()))!=='')
			$writer->addStyleAttribute('background-image','url('.$url.')');

		if(($horizontalAlign=$this->getHorizontalAlign())!==THorizontalAlign::NotSet)
			$writer->addStyleAttribute('text-align',strtolower($horizontalAlign));

		if(($cellPadding=$this->getCellPadding())>=0)
			$writer->addAttribute('cellpadding',"$cellPadding");

		if(($cellSpacing=$this->getCellSpacing())>=0)
			$writer->addAttribute('cellspacing',"$cellSpacing");

		if($this->getBorderCollapse())
			$writer->addStyleAttribute('border-collapse','collapse');

		switch($this->getGridLines())
		{
			case TTableGridLines::Horizontal : $writer->addAttribute('rules','rows'); break;
			case TTableGridLines::Vertical : $writer->addAttribute('rules','cols'); break;
			case TTableGridLines::Both : $writer->addAttribute('rules','all'); break;
		}

		parent::addAttributesToRender($writer);
	}

	/**
	 * @return string the URL of the background image for the table
	 */
	public function getBackImageUrl()
	{
		return $this->_backImageUrl===null?'':$this->_backImageUrl;
	}

	/**
	 * Sets the URL of the background image for the table
	 * @param string the URL
	 */
	public function setBackImageUrl($value)
	{
		$this->_backImageUrl=$value;
	}

	/**
	 * @return THorizontalAlign the horizontal alignment of the contents within the table, defaults to THorizontalAlign::NotSet.
	 */
	public function getHorizontalAlign()
	{
		return $this->_horizontalAlign===null?THorizontalAlign::NotSet:$this->_horizontalAlign;
	}

	/**
	 * Sets the horizontal alignment of the contents within the table.
	 * @param THorizontalAlign the horizontal alignment
	 */
	public function setHorizontalAlign($value)
	{
		$this->_horizontalAlign=TPropertyValue::ensureEnum($value,'THorizontalAlign');
	}

	/**
	 * @return integer cellpadding of the table. Defaults to -1, meaning not set.
	 */
	public function getCellPadding()
	{
		return $this->_cellPadding===null?-1:$this->_cellPadding;
	}

	/**
	 * @param integer cellpadding of the table. A value equal to -1 clears up the setting.
	 * @throws TInvalidDataValueException if the value is less than -1.
	 */
	public function setCellPadding($value)
	{
		if(($this->_cellPadding=TPropertyValue::ensureInteger($value))<-1)
			throw new TInvalidDataValueException('tablestyle_cellpadding_invalid');
	}

	/**
	 * @return integer cellspacing of the table. Defaults to -1, meaning not set.
	 */
	public function getCellSpacing()
	{
		return $this->_cellSpacing===null?-1:$this->_cellSpacing;
	}

	/**
	 * @param integer cellspacing of the table. A value equal to -1 clears up the setting.
	 * @throws TInvalidDataValueException if the value is less than -1.
	 */
	public function setCellSpacing($value)
	{
		if(($this->_cellSpacing=TPropertyValue::ensureInteger($value))<-1)
			throw new TInvalidDataValueException('tablestyle_cellspacing_invalid');
	}

	/**
	 * @return TTableGridLines the grid line setting of the table. Defaults to TTableGridLines::None.
	 */
	public function getGridLines()
	{
		return $this->_gridLines===null?TTableGridLines::None:$this->_gridLines;
	}

	/**
	 * Sets the grid line style of the table.
	 * @param TTableGridLines the grid line setting of the table
	 */
	public function setGridLines($value)
	{
		$this->_gridLines=TPropertyValue::ensureEnum($value,'TTableGridLines');
	}


	/**
	 * @return boolean whether the table borders should be collapsed. Defaults to false.
	 */
	public function getBorderCollapse()
	{
		return $this->_borderCollapse===null?false:$this->_borderCollapse;
	}

	/**
	 * @param boolean whether the table borders should be collapsed.
	 */
	public function setBorderCollapse($value)
	{
		$this->_borderCollapse=TPropertyValue::ensureBoolean($value);
	}
}

/**
 * TTableItemStyle class.
 * TTableItemStyle represents the CSS style specific for HTML table item.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTableItemStyle extends TStyle
{
	/**
	 * @var THorizontalAlign horizontal alignment of the contents within the table item
	 */
	private $_horizontalAlign=null;
	/**
	 * @var TVerticalAlign vertical alignment of the contents within the table item
	 */
	private $_verticalAlign=null;
	/**
	 * @var boolean whether the content wraps within the table item
	 */
	private $_wrap=null;

	/**
	 * Sets the style attributes to default values.
	 * This method overrides the parent implementation by
	 * resetting additional TTableItemStyle specific attributes.
	 */
	public function reset()
	{
		parent::reset();
		$this->_verticalAlign=null;
		$this->_horizontalAlign=null;
		$this->_wrap=null;
	}

	/**
	 * Copies the fields in a new style to this style.
	 * If a style field is set in the new style, the corresponding field
	 * in this style will be overwritten.
	 * @param TStyle the new style
	 */
	public function copyFrom($style)
	{
		parent::copyFrom($style);
		if($style instanceof TTableItemStyle)
		{
			if($this->_verticalAlign===null && $style->_verticalAlign!==null)
				$this->_verticalAlign=$style->_verticalAlign;
			if($this->_horizontalAlign===null && $style->_horizontalAlign!==null)
				$this->_horizontalAlign=$style->_horizontalAlign;
			if($this->_wrap===null && $style->_wrap!==null)
				$this->_wrap=$style->_wrap;
		}
	}

	/**
	 * Merges the style with a new one.
	 * If a style field is not set in this style, it will be overwritten by
	 * the new one.
	 * @param TStyle the new style
	 */
	public function mergeWith($style)
	{
		parent::mergeWith($style);
		if($style instanceof TTableItemStyle)
		{
			if($style->_verticalAlign!==null)
				$this->_verticalAlign=$style->_verticalAlign;
			if($style->_horizontalAlign!==null)
				$this->_horizontalAlign=$style->_horizontalAlign;
			if($style->_wrap!==null)
				$this->_wrap=$style->_wrap;
		}
	}

	/**
	 * Adds attributes related to CSS styles to renderer.
	 * This method overrides the parent implementation.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function addAttributesToRender($writer)
	{
		if(!$this->getWrap())
			$writer->addStyleAttribute('white-space','nowrap');

		if(($horizontalAlign=$this->getHorizontalAlign())!==THorizontalAlign::NotSet)
			$writer->addAttribute('align',strtolower($horizontalAlign));

		if(($verticalAlign=$this->getVerticalAlign())!==TVerticalAlign::NotSet)
			$writer->addAttribute('valign',strtolower($verticalAlign));

		parent::addAttributesToRender($writer);
	}

	/**
	 * @return THorizontalAlign the horizontal alignment of the contents within the table item, defaults to THorizontalAlign::NotSet.
	 */
	public function getHorizontalAlign()
	{
		return $this->_horizontalAlign===null?THorizontalAlign::NotSet:$this->_horizontalAlign;
	}

	/**
	 * Sets the horizontal alignment of the contents within the table item.
	 * @param THorizontalAlign the horizontal alignment
	 */
	public function setHorizontalAlign($value)
	{
		$this->_horizontalAlign=TPropertyValue::ensureEnum($value,'THorizontalAlign');
	}

	/**
	 * @return TVerticalAlign the vertical alignment of the contents within the table item, defaults to TVerticalAlign::NotSet.
	 */
	public function getVerticalAlign()
	{
		return $this->_verticalAlign===null?TVerticalAlign::NotSet:$this->_verticalAlign;
	}

	/**
	 * Sets the vertical alignment of the contents within the table item.
	 * @param TVerticalAlign the horizontal alignment
	 */
	public function setVerticalAlign($value)
	{
		$this->_verticalAlign=TPropertyValue::ensureEnum($value,'TVerticalAlign');
	}

	/**
	 * @return boolean whether the content wraps within the table item. Defaults to true.
	 */
	public function getWrap()
	{
		return $this->_wrap===null?true:$this->_wrap;
	}

	/**
	 * Sets the value indicating whether the content wraps within the table item.
	 * @param boolean whether the content wraps within the panel.
	 */
	public function setWrap($value)
	{
		$this->_wrap=TPropertyValue::ensureBoolean($value);
	}
}

/**
 * THorizontalAlign class.
 * THorizontalAlign defines the enumerable type for the possible horizontal alignments in a CSS style.
 *
 * The following enumerable values are defined:
 * - NotSet: the alignment is not specified.
 * - Left: left aligned
 * - Right: right aligned
 * - Center: center aligned
 * - Justify: the begin and end are justified
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class THorizontalAlign extends TEnumerable
{
	const NotSet='NotSet';
	const Left='Left';
	const Right='Right';
	const Center='Center';
	const Justify='Justify';
}

/**
 * TVerticalAlign class.
 * TVerticalAlign defines the enumerable type for the possible vertical alignments in a CSS style.
 *
 * The following enumerable values are defined:
 * - NotSet: the alignment is not specified.
 * - Top: top aligned
 * - Bottom: bottom aligned
 * - Middle: middle aligned
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TVerticalAlign extends TEnumerable
{
	const NotSet='NotSet';
	const Top='Top';
	const Bottom='Bottom';
	const Middle='Middle';
}


/**
 * TTableGridLines class.
 * TTableGridLines defines the enumerable type for the possible grid line types of an HTML table.
 *
 * The following enumerable values are defined:
 * - None: no grid lines
 * - Horizontal: horizontal grid lines only
 * - Vertical: vertical grid lines only
 * - Both: both horizontal and vertical grid lines are shown
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TStyle.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TTableGridLines extends TEnumerable
{
	const None='None';
	const Horizontal='Horizontal';
	const Vertical='Vertical';
	const Both='Both';
}

