<?php
/**
 * THiddenField class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.xisc.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.opensource.org/licenses/bsd-license.php BSD License
 * @version $Id: THiddenField.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * THiddenField class
 *
 * THiddenField displays a hidden input field on a Web page.
 * The value of the input field can be accessed via {@link getValue Value} property.
 * If upon postback the value is changed, a {@link onValueChanged OnValueChanged}
 * event will be raised.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THiddenField.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class THiddenField extends TControl implements IPostBackDataHandler, IValidatable, IDataRenderer
{
	private $_dataChanged=false;
	private $_isValid=true;

	/**
	 * @return string tag name of the hidden field.
	 */
	protected function getTagName()
	{
		return 'input';
	}

	/**
	 * Sets focus to this control.
	 * This method overrides the parent implementation by forbidding setting focus to this control.
	 */
	public function focus()
	{
		throw new TNotSupportedException('hiddenfield_focus_unsupported');
	}

	/**
	 * Renders the control.
	 * This method overrides the parent implementation by rendering
	 * the hidden field input element.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function render($writer)
	{
		$uniqueID=$this->getUniqueID();
		$this->getPage()->ensureRenderInForm($this);
		$writer->addAttribute('type','hidden');
		if($uniqueID!=='')
			$writer->addAttribute('name',$uniqueID);
		if($this->getID()!=='')
			$writer->addAttribute('id',$this->getClientID());
		if(($value=$this->getValue())!=='')
			$writer->addAttribute('value',$value);

		if($this->getHasAttributes())
		{
			foreach($this->getAttributes() as $name=>$value)
				$writer->addAttribute($name,$value);
		}

		$writer->renderBeginTag('input');
		$writer->renderEndTag();
	}

	/**
	 * Loads hidden field data.
	 * This method is primarly used by framework developers.
	 * @param string the key that can be used to retrieve data from the input data collection
	 * @param array the input data collection
	 * @return boolean whether the data of the component has been changed
	 */
	public function loadPostData($key,$values)
	{
		$value=$values[$key];
		if($value===$this->getValue())
			return false;
		else
		{
			$this->setValue($value);
			return $this->_dataChanged=true;
		}
	}

	/**
	 * Returns a value indicating whether postback has caused the control data change.
	 * This method is required by the IPostBackDataHandler interface.
	 * @return boolean whether postback has caused the control data change. False if the page is not in postback mode.
	 */
	public function getDataChanged()
	{
		return $this->_dataChanged;
	}

	/**
	 * Returns the value to be validated.
	 * This methid is required by IValidatable interface.
	 * @return mixed the value of the property to be validated.
	 */
	public function getValidationPropertyValue()
	{
		return $this->getValue();
	}

	/**
	 * Returns true if this control validated successfully. 
	 * Defaults to true.
	 * @return bool wether this control validated successfully.
	 */
	public function getIsValid()
	{
	    return $this->_isValid;
	}
	/**
	 * @param bool wether this control is valid.
	 */
	public function setIsValid($value)
	{
	    $this->_isValid=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * Raises postdata changed event.
	 * This method calls {@link onValueChanged} method.
	 * This method is primarly used by framework developers.
	 */
	public function raisePostDataChangedEvent()
	{
		$this->onValueChanged(null);
	}

	/**
	 * This method is invoked when the value of the {@link getValue Value} property changes between posts to the server.
	 * The method raises 'OnValueChanged' event to fire up the event delegates.
	 * If you override this method, be sure to call the parent implementation
	 * so that the attached event handlers can be invoked.
	 * @param TEventParameter event parameter to be passed to the event handlers
	 */
	public function onValueChanged($param)
	{
		$this->raiseEvent('OnValueChanged',$this,$param);
	}

	/**
	 * @return string the value of the THiddenField
	 */
	public function getValue()
	{
		return $this->getViewState('Value','');
	}

	/**
	 * Sets the value of the THiddenField
	 * @param string the value to be set
	 */
	public function setValue($value)
	{
		$this->setViewState('Value',$value,'');
	}

	/**
	 * Returns the value of the hidden field.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getValue()}.
	 * @return string value of the hidden field
	 * @see getValue
	 * @since 3.1.0
	 */
	public function getData()
	{
		return $this->getValue();
	}

	/**
	 * Sets the value of the hidden field.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setValue()}.
	 * @param string value of the hidden field
	 * @see setValue
	 * @since 3.1.0
	 */
	public function setData($value)
	{
		$this->setValue($value);
	}


	/**
	 * @return boolean whether theming is enabled for this control. Defaults to false.
	 */
	public function getEnableTheming()
	{
		return false;
	}

	/**
	 * @param boolean whether theming is enabled for this control.
	 * @throws TNotSupportedException This method is always thrown when calling this method.
	 */
	public function setEnableTheming($value)
	{
		throw new TNotSupportedException('hiddenfield_theming_unsupported');
	}

	/**
	 * @param string Skin ID
	 * @throws TNotSupportedException This method is always thrown when calling this method.
	 */
	public function setSkinID($value)
	{
		throw new TNotSupportedException('hiddenfield_skinid_unsupported');
	}
}

