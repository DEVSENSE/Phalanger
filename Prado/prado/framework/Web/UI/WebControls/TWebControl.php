<?php
/**
 * TWebControl class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TWebControl.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TStyle and TWebAdapter definition
 */
Prado::using('System.Web.UI.WebControls.TStyle');
Prado::using('System.Web.UI.WebControls.TWebControlAdapter');

/**
 * TWebControl class
 *
 * TWebControl is the base class for controls that share a common set
 * of UI-related properties and methods. TWebControl-derived controls
 * are usually associated with HTML tags. They thus have tag name, attributes
 * and body contents. You can override {@link getTagName} to specify the tag name,
 * {@link addAttributesToRender} to specify the attributes to be rendered,
 * and {@link renderContents} to customize the body content rendering.
 * TWebControl encapsulates a set of properties related with CSS style fields,
 * such as {@link getBackColor BackColor}, {@link getBorderWidth BorderWidth}, etc.
 *
 * Subclasses of TWebControl typically needs to override {@link addAttributesToRender}
 * and {@link renderContents}. The former is used to render the attributes
 * of the HTML tag associated with the control, while the latter is to render
 * the body contents enclosed within the HTML tag.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWebControl.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWebControl extends TControl implements IStyleable
{
	/**
	 * Copies basic control attributes from another control.
	 * Properties including AccessKey, ToolTip, TabIndex, Enabled
	 * and Attributes are copied.
	 * @param TWebControl source control
	 */
	public function copyBaseAttributes(TWebControl $control)
	{
		$this->setAccessKey($control->getAccessKey());
		$this->setToolTip($control->getToolTip());
		$this->setTabIndex($control->getTabIndex());
		if(!$control->getEnabled())
			$this->setEnabled(false);
		if($control->getHasAttributes())
			$this->getAttributes()->copyFrom($control->getAttributes());
	}

	/**
	 * @return string the access key of the control
	 */
	public function getAccessKey()
	{
		return $this->getViewState('AccessKey','');
	}

	/**
	 * Sets the access key of the control.
	 * Only one-character string can be set, or an exception will be raised.
	 * Pass in an empty string if you want to disable access key.
	 * @param string the access key to be set
	 * @throws TInvalidDataValueException if the access key is specified with more than one character
	 */
	public function setAccessKey($value)
	{
		if(strlen($value)>1)
			throw new TInvalidDataValueException('webcontrol_accesskey_invalid',get_class($this),$value);
		$this->setViewState('AccessKey',$value,'');
	}

	/**
	 * @return string the background color of the control
	 */
	public function getBackColor()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getBackColor();
		else
			return '';
	}

	/**
	 * @param string the background color of the control
	 */
	public function setBackColor($value)
	{
		$this->getStyle()->setBackColor($value);
	}

	/**
	 * @return string the border color of the control
	 */
	public function getBorderColor()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getBorderColor();
		else
			return '';
	}

	/**
	 * @param string the border color of the control
	 */
	public function setBorderColor($value)
	{
		$this->getStyle()->setBorderColor($value);
	}

	/**
	 * @return string the border style of the control
	 */
	public function getBorderStyle()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getBorderStyle();
		else
			return '';
	}

	/**
	 * @param string the border style of the control
	 */
	public function setBorderStyle($value)
	{
		$this->getStyle()->setBorderStyle($value);
	}

	/**
	 * @return string the border width of the control
	 */
	public function getBorderWidth()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getBorderWidth();
		else
			return '';
	}

	/**
	 * @param string the border width of the control
	 */
	public function setBorderWidth($value)
	{
		$this->getStyle()->setBorderWidth($value);
	}

	/**
	 * @return TFont the font of the control
	 */
	public function getFont()
	{
		return $this->getStyle()->getFont();
	}

	/**
	 * @return string the foreground color of the control
	 */
	public function getForeColor()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getForeColor();
		else
			return '';
	}

	/**
	 * @param string the foreground color of the control
	 */
	public function setForeColor($value)
	{
		$this->getStyle()->setForeColor($value);
	}

	/**
	 * @return string the height of the control
	 */
	public function getHeight()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getHeight();
		else
			return '';
	}

	/**
	 * @param TDisplayStyle display style of the control, default is TDisplayStyle::Fixed
	 */
	public function setDisplay($value)
	{
		$this->getStyle()->setDisplayStyle($value);
	}

	/**
	 * @return TDisplayStyle display style of the control, default is TDisplayStyle::Fixed
	 */
	public function getDisplay()
	{
		return $this->getStyle()->getDisplayStyle();
	}

	/**
	 * @param string the css class of the control
	 */
	public function setCssClass($value)
	{
		$this->getStyle()->setCssClass($value);
	}

	/**
	 * @return string the css class of the control
	 */
	public function getCssClass()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getCssClass();
		else
			return '';
	}

	/**
	 * @param string the height of the control
	 */
	public function setHeight($value)
	{
		$this->getStyle()->setHeight($value);
	}

	/**
	 * @return boolean whether the control has defined any style information
	 */
	public function getHasStyle()
	{
		return $this->getViewState('Style',null)!==null;
	}

	/**
	 * Creates a style object to be used by the control.
	 * This method may be overriden by controls to provide customized style.
	 * @return TStyle the default style created for TWebControl
	 */
	protected function createStyle()
	{
		return new TStyle;
	}

	/**
	 * @return TStyle the object representing the css style of the control
	 */
	public function getStyle()
	{
		if($style=$this->getViewState('Style',null))
			return $style;
		else
		{
			$style=$this->createStyle();
			$this->setViewState('Style',$style,null);
			return $style;
		}
	}

	/**
	 * Sets the css style string of the control.
	 * The style string will be prefixed to the styles set via other control properties (e.g. Height, Width).
	 * @param string the css style string
	 * @throws TInvalidDataValueException if the parameter is not a string
	 */
	public function setStyle($value)
	{
		if(is_string($value))
			$this->getStyle()->setCustomStyle($value);
		else
			throw new TInvalidDataValueException('webcontrol_style_invalid',get_class($this));
	}

	/**
	 * Removes all style data.
	 */
	public function clearStyle()
	{
		$this->clearViewState('Style');
	}

	/**
	 * @return integer the tab index of the control
	 */
	public function getTabIndex()
	{
		return $this->getViewState('TabIndex',0);
	}

	/**
	 * Sets the tab index of the control.
	 * Pass 0 if you want to disable tab index.
	 * @param integer the tab index to be set
	 */
	public function setTabIndex($value)
	{
		$this->setViewState('TabIndex',TPropertyValue::ensureInteger($value),0);
	}

	/**
	 * Returns the tag name used for this control.
	 * By default, the tag name is 'span'.
	 * You can override this method to provide customized tag names.
	 * @return string tag name of the control to be rendered
	 */
	protected function getTagName()
	{
		return 'span';
	}

	/**
	 * @return string the tooltip of the control
	 */
	public function getToolTip()
	{
		return $this->getViewState('ToolTip','');
	}

	/**
	 * Sets the tooltip of the control.
	 * Pass an empty string if you want to disable tooltip.
	 * @param string the tooltip to be set
	 */
	public function setToolTip($value)
	{
		$this->setViewState('ToolTip',$value,'');
	}

	/**
	 * @return string the width of the control
	 */
	public function getWidth()
	{
		if($style=$this->getViewState('Style',null))
			return $style->getWidth();
		else
			return '';
	}

	/**
	 * @param string the width of the control
	 */
	public function setWidth($value)
	{
		$this->getStyle()->setWidth($value);
	}

	/**
	 * Adds attribute name-value pairs to renderer.
	 * By default, the method will render 'id', 'accesskey', 'disabled',
	 * 'tabindex', 'title' and all custom attributes.
	 * The method can be overriden to provide customized attribute rendering.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		if($this->getID()!=='')
			$writer->addAttribute('id',$this->getClientID());
		if(($accessKey=$this->getAccessKey())!=='')
			$writer->addAttribute('accesskey',$accessKey);
		if(!$this->getEnabled())
			$writer->addAttribute('disabled','disabled');
		if(($tabIndex=$this->getTabIndex())>0)
			$writer->addAttribute('tabindex',"$tabIndex");
		if(($toolTip=$this->getToolTip())!=='')
			$writer->addAttribute('title',$toolTip);
		if($style=$this->getViewState('Style',null))
			$style->addAttributesToRender($writer);
		if($this->getHasAttributes())
		{
			foreach($this->getAttributes() as $name=>$value)
				$writer->addAttribute($name,$value);
		}
	}

	/**
	 * Renders the control.
	 * This method overrides the parent implementation by replacing it with
	 * the following sequence:
	 * - {@link renderBeginTag}
	 * - {@link renderContents}
	 * - {@link renderEndTag}
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function render($writer)
	{
		$this->renderBeginTag($writer);
		$this->renderContents($writer);
		$this->renderEndTag($writer);
	}

	/**
	 * Renders the openning tag for the control (including attributes)
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function renderBeginTag($writer)
	{
		$this->addAttributesToRender($writer);
		$writer->renderBeginTag($this->getTagName());
	}

	/**
	 * Renders the body content enclosed between the control tag.
	 * By default, child controls and text strings will be rendered.
	 * You can override this method to provide customized content rendering.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function renderContents($writer)
	{
		parent::renderChildren($writer);
	}

	/**
	 * Renders the closing tag for the control
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function renderEndTag($writer)
	{
		$writer->renderEndTag();
	}
}

