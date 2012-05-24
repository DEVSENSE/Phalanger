<?php
/**
 * TWizardNavigationButtonStyle class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TStyle class file
 */
Prado::using('System.Web.UI.WebControls.TStyle');

/**
 * TWizardNavigationButtonStyle class.
 * TWizardNavigationButtonStyle defines the style applied to a wizard navigation button.
 * The button type can be specified via {@link setButtonType ButtonType}, which
 * can be 'Button', 'Image' or 'Link'.
 * If the button is an image button, {@link setImageUrl ImageUrl} will be
 * used to load the image for the button.
 * Otherwise, {@link setButtonText ButtonText} will be displayed as the button caption.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWizardNavigationButtonStyle.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWizardNavigationButtonStyle extends TStyle
{
	private $_imageUrl=null;
	private $_buttonText=null;
	private $_buttonType=null;

	/**
	 * Sets the style attributes to default values.
	 * This method overrides the parent implementation by
	 * resetting additional TWizardNavigationButtonStyle specific attributes.
	 */
	public function reset()
	{
		parent::reset();
		$this->_imageUrl=null;
		$this->_buttonText=null;
		$this->_buttonType=null;
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
		if($style instanceof TWizardNavigationButtonStyle)
		{
			if($this->_imageUrl===null && $style->_imageUrl!==null)
				$this->_imageUrl=$style->_imageUrl;
			if($this->_buttonText===null && $style->_buttonText!==null)
				$this->_buttonText=$style->_buttonText;
			if($this->_buttonType===null && $style->_buttonType!==null)
				$this->_buttonType=$style->_buttonType;
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
		if($style instanceof TWizardNavigationButtonStyle)
		{
			if($style->_imageUrl!==null)
				$this->_imageUrl=$style->_imageUrl;
			if($style->_buttonText!==null)
				$this->_buttonText=$style->_buttonText;
			if($style->_buttonType!==null)
				$this->_buttonType=$style->_buttonType;
		}
	}

	/**
	 * @return string image URL for the image button
	 */
	public function getImageUrl()
	{
		return $this->_imageUrl===null?'':$this->_imageUrl;
	}

	/**
	 * @param string image URL for the image button
	 */
	public function setImageUrl($value)
	{
		$this->_imageUrl=$value;
	}

	/**
	 * @return string button caption
	 */
	public function getButtonText()
	{
		return $this->_buttonText===null?'':$this->_buttonText;
	}

	/**
	 * @param string button caption
	 */
	public function setButtonText($value)
	{
		$this->_buttonText=$value;
	}

	/**
	 * @return TWizardNavigationButtonType button type. Default to TWizardNavigationButtonType::Button.
	 */
	public function getButtonType()
	{
		return $this->_buttonType===null? TWizardNavigationButtonType::Button :$this->_buttonType;
	}

	/**
	 * @param TWizardNavigationButtonType button type.
	 */
	public function setButtonType($value)
	{
		$this->_buttonType=TPropertyValue::ensureEnum($value,'TWizardNavigationButtonType');
	}

	/**
	 * Applies this style to the specified button
	 * @param mixed button to be applied with this style
	 */
	public function apply($button)
	{
		if($button instanceof TImageButton)
		{
			if($button->getImageUrl()==='')
				$button->setImageUrl($this->getImageUrl());
		}
		if($button->getText()==='')
			$button->setText($this->getButtonText());
		$button->getStyle()->mergeWith($this);
	}
}

