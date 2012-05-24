<?php
/**
 * TPanelStyle class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPanelStyle.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TStyle class file
 */
Prado::using('System.Web.UI.WebControls.TStyle');

/**
 * TPanelStyle class.
 * TPanelStyle represents the CSS style specific for panel HTML tag.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TPanelStyle.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TPanelStyle extends TStyle
{
	/**
	 * @var string the URL of the background image for the panel component
	 */
	private $_backImageUrl=null;
	/**
	 * @var string alignment of the content in the panel.
	 */
	private $_direction=null;
	/**
	 * @var string horizontal alignment of the contents within the panel
	 */
	private $_horizontalAlign=null;
	/**
	 * @var string visibility and position of scroll bars
	 */
	private $_scrollBars=null;
	/**
	 * @var boolean whether the content wraps within the panel
	 */
	private $_wrap=null;

	/**
	 * Adds attributes related to CSS styles to renderer.
	 * This method overrides the parent implementation.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function addAttributesToRender($writer)
	{
		if(($url=trim($this->getBackImageUrl()))!=='')
			$this->setStyleField('background-image','url('.$url.')');

		switch($this->getScrollBars())
		{
			case TScrollBars::Horizontal: $this->setStyleField('overflow-x','scroll'); break;
			case TScrollBars::Vertical: $this->setStyleField('overflow-y','scroll'); break;
			case TScrollBars::Both: $this->setStyleField('overflow','scroll'); break;
			case TScrollBars::Auto: $this->setStyleField('overflow','auto'); break;
		}

		if(($align=$this->getHorizontalAlign())!==THorizontalAlign::NotSet)
			$this->setStyleField('text-align',strtolower($align));

		if(!$this->getWrap())
			$this->setStyleField('white-space','nowrap');

		if(($direction=$this->getDirection())!==TContentDirection::NotSet)
		{
			if($direction===TContentDirection::LeftToRight)
				$this->setStyleField('direction','ltr');
			else
				$this->setStyleField('direction','rtl');
		}

		parent::addAttributesToRender($writer);
	}

	/**
	 * @return string the URL of the background image for the panel component.
	 */
	public function getBackImageUrl()
	{
		return $this->_backImageUrl===null?'':$this->_backImageUrl;
	}

	/**
	 * Sets the URL of the background image for the panel component.
	 * @param string the URL
	 */
	public function setBackImageUrl($value)
	{
		$this->_backImageUrl=$value;
	}

	/**
	 * @return TContentDirection alignment of the content in the panel. Defaults to TContentDirection::NotSet.
	 */
	public function getDirection()
	{
		return $this->_direction===null?TContentDirection::NotSet:$this->_direction;
	}

	/**
	 * @param TContentDirection alignment of the content in the panel.
	 */
	public function setDirection($value)
	{
		$this->_direction=TPropertyValue::ensureEnum($value,'TContentDirection');
	}

	/**
	 * @return boolean whether the content wraps within the panel. Defaults to true.
	 */
	public function getWrap()
	{
		return $this->_wrap===null?true:$this->_wrap;
	}

	/**
	 * Sets the value indicating whether the content wraps within the panel.
	 * @param boolean whether the content wraps within the panel.
	 */
	public function setWrap($value)
	{
		$this->_wrap=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return THorizontalAlign the horizontal alignment of the contents within the panel, defaults to THorizontalAlign::NotSet.
	 */
	public function getHorizontalAlign()
	{
		return $this->_horizontalAlign===null?THorizontalAlign::NotSet:$this->_horizontalAlign;
	}

	/**
	 * Sets the horizontal alignment of the contents within the panel.
	 * @param THorizontalAlign the horizontal alignment
	 */
	public function setHorizontalAlign($value)
	{
		$this->_horizontalAlign=TPropertyValue::ensureEnum($value,'THorizontalAlign');
	}

	/**
	 * @return TScrollBars the visibility and position of scroll bars in a panel control, defaults to TScrollBars::None.
	 */
	public function getScrollBars()
	{
		return $this->_scrollBars===null?TScrollBars::None:$this->_scrollBars;
	}

	/**
	 * @param TScrollBars the visibility and position of scroll bars in a panel control.
	 */
	public function setScrollBars($value)
	{
		$this->_scrollBars=TPropertyValue::ensureEnum($value,'TScrollBars');
	}

	/**
	 * Sets the style attributes to default values.
	 * This method overrides the parent implementation by
	 * resetting additional TPanelStyle specific attributes.
	 */
	public function reset()
	{
		parent::reset();
		$this->_backImageUrl=null;
		$this->_direction=null;
		$this->_horizontalAlign=null;
		$this->_scrollBars=null;
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
		if($style instanceof TPanelStyle)
		{
			if($style->_backImageUrl!==null)
				$this->_backImageUrl=$style->_backImageUrl;
			if($style->_direction!==null)
				$this->_direction=$style->_direction;
			if($style->_horizontalAlign!==null)
				$this->_horizontalAlign=$style->_horizontalAlign;
			if($style->_scrollBars!==null)
				$this->_scrollBars=$style->_scrollBars;
			if($style->_wrap!==null)
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
		if($style instanceof TPanelStyle)
		{
			if($this->_backImageUrl===null && $style->_backImageUrl!==null)
				$this->_backImageUrl=$style->_backImageUrl;
			if($this->_direction===null && $style->_direction!==null)
				$this->_direction=$style->_direction;
			if($this->_horizontalAlign===null && $style->_horizontalAlign!==null)
				$this->_horizontalAlign=$style->_horizontalAlign;
			if($this->_scrollBars===null && $style->_scrollBars!==null)
				$this->_scrollBars=$style->_scrollBars;
			if($this->_wrap===null && $style->_wrap!==null)
				$this->_wrap=$style->_wrap;
		}
	}
}

/**
 * TContentDirection class.
 * TContentDirection defines the enumerable type for the possible directions that a panel can be at.
 *
 * The following enumerable values are defined:
 * - NotSet: the direction is not specified
 * - LeftToRight: content in a panel is left to right
 * - RightToLeft: content in a panel is right to left
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TPanelStyle.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TContentDirection extends TEnumerable
{
	const NotSet='NotSet';
	const LeftToRight='LeftToRight';
	const RightToLeft='RightToLeft';
}

/**
 * TScrollBars class.
 * TScrollBars defines the enumerable type for the possible scroll bar mode
 * that a {@link TPanel} control could use.
 *
 * The following enumerable values are defined:
 * - None: no scroll bars.
 * - Auto: scroll bars automatically appeared when needed.
 * - Both: show both horizontal and vertical scroll bars all the time.
 * - Horizontal: horizontal scroll bar only
 * - Vertical: vertical scroll bar only
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TPanelStyle.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TScrollBars extends TEnumerable
{
	const None='None';
	const Auto='Auto';
	const Both='Both';
	const Horizontal='Horizontal';
	const Vertical='Vertical';
}

