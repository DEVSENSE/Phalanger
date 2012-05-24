<?php
/**
 * TInPlaceTextBox class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TInPlaceTextBox.php 2765 2010-02-15 08:47:50Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */

Prado::using('System.Web.UI.ActiveControls.TActiveTextBox');

/**
 * TInPlaceTextBox Class
 * *
 * TInPlaceTextBox is a component rendered as a label and allows its
 * contents to be edited by changing the label to a textbox when
 * the label is clicked or when another control or html element with
 * ID given by {@link setEditTriggerControlID EditTriggerControlID} is clicked.
 *
 * If the {@link OnLoadingText} event is handled, a callback request is
 * made when the label is clicked, while the request is being made the
 * textbox is disabled from editing. The {@link OnLoadingText} event allows
 * you to update the content of the textbox before the client is allowed
 * to edit the content. After the callback request returns successfully,
 * the textbox is enabled and the contents is then allowed to be edited.
 *
 * Once the textbox loses focus, if {@link setAutoPostBack AutoPostBack}
 * is true and the textbox content has changed, a callback request is made and
 * the {@link OnTextChanged} event is raised like that of the TActiveTextBox.
 * During the request, the textbox is disabled.
 *
 * After the callback request returns sucessfully, the textbox is enabled.
 * If the {@link setAutoHideTextBox AutoHideTextBox} property is true, then
 * the textbox will be hidden and the label is then shown.
 *
 * Since 3.1.2, you can set the {@link setReadOnly ReadOnly} property to make
 * the control not editable. This property can be also changed on callback
 * 
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TInPlaceTextBox.php 2765 2010-02-15 08:47:50Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TInPlaceTextBox extends TActiveTextBox
{
	/**
	 * Sets the auto post back to true by default.
	 */
	public function __construct()
	{
		parent::__construct();
		$this->setAutoPostBack(true);
	}

	/**
	 * @param boolean true to hide the textbox after losing focus.
	 */
	public function setAutoHideTextBox($value)
	{
		$this->setViewState('AutoHide', TPropertyValue::ensureBoolean($value), true);
	}

	/**
	 * @return boolean true will hide the textbox after losing focus.
	 */
	public function getAutoHideTextBox()
	{
		return $this->getViewState('AutoHide', true);
	}

	/**
	 * @param boolean true to display the edit textbox
	 */
	public function setDisplayTextBox($value)
	{
		$value = TPropertyValue::ensureBoolean($value);
		$this->setViewState('DisplayTextBox', $value,false);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->callClientFunction('setDisplayTextBox',$value);
	}

	/**
	 * @return boolean true to display the edit textbox
	 */
	public function getDisplayTextBox()
	{
		return $this->getViewState('DisplayTextBox', false);
	}

	/**
	 * Calls the client-side static method for this control class.
	 * @param string static method name
	 * @param mixed method parmaeter
	 */
	protected function callClientFunction($func,$value)
	{
		$client = $this->getPage()->getCallbackClient();
		$code = $this->getClientClassName().'.'.$func;
		$client->callClientFunction($code,array($this,$value));
	}

	/**
	 * @param string ID of the control that can trigger to edit the textbox
	 */
	public function setEditTriggerControlID($value)
	{
		$this->setViewState('EditTriggerControlID', $value);
	}

	/**
	 * @return string ID of the control that can trigger to edit the textbox
	 */
	public function getEditTriggerControlID()
	{
		return $this->getViewState('EditTriggerControlID');
	}

	/**
	 * @return string edit trigger control client ID.
	 */
	protected function getExternalControlID()
	{
		$extID = $this->getEditTriggerControlID();
		if($extID===null) return '';
		if(($control = $this->findControl($extID))!==null)
			return $control->getClientID();
		return $extID;
	}

	/**
	 * On callback response, the inner HTMl of the label and the
	 * value of the textbox is updated
	 * @param string the text value of the label
	 */
	public function setText($value)
	{
		TTextBox::setText($value);
		if($this->getActiveControl()->canUpdateClientSide())
		{
			$client = $this->getPage()->getCallbackClient();
			$client->update($this->getLabelClientID(), $value);
			$client->setValue($this, $value);
		}
	}
	
	/**
	 * Update ClientSide Readonly property
	 * @param boolean value
	 * @since 3.1.2
	 */
	public function setReadOnly ($value)
	{
		$value=TPropertyValue::ensureBoolean($value);
		TTextBox::setReadOnly($value);
		if ($this->getActiveControl()->canUpdateClientSide())
		{
			$this->callClientFunction('setReadOnly', $value);
		}
	}

	/**
	 * @return string tag name of the label.
	 */
	protected function getTagName()
	{
		return 'span';
	}

	/**
	 * Renders the body content of the label.
	 * @param THtmlWriter the writer for rendering
	 */
	public function renderContents($writer)
	{
		if(($text=$this->getText())==='')
			parent::renderContents($writer);
		else
			$writer->write($text);
	}

	/**
	 * @return string label client ID
	 */
	protected function getLabelClientID()
	{
		return $this->getClientID().'__label';
	}

	/**
	 * This method is invoked when a callback is requested. The method raises
	 * 'OnCallback' event to fire up the event handlers. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onCallback($param)
	{
		$action = $param->getCallbackParameter();
		if(is_array($action) && $action[0] === '__InlineEditor_loadExternalText__')
		{
			$parameter = new TCallbackEventParameter($this->getResponse(), $action[1]);
			$this->onLoadingText($parameter);
		}
		$this->raiseEvent('OnCallback', $this, $param);
	}

	/**
	 * @return array callback options.
	 */
	protected function getPostBackOptions()
	{
		$options = parent::getPostBackOptions();
		$options['ID'] = $this->getLabelClientID();
		$options['TextBoxID'] = $this->getClientID();
		$options['ExternalControl'] = $this->getExternalControlID();
		$options['AutoHide'] = $this->getAutoHideTextBox() == false ? '' : true;
		$options['AutoPostBack'] = $this->getAutoPostBack() == false ? '' : true;
		$options['Columns'] = $this->getColumns();
		if($this->getTextMode()==='MultiLine')
		{
			$options['Rows'] = $this->getRows();
			$options['Wrap'] = $this->getWrap()== false ? '' : true;
		}
		else
		{
			$length = $this->getMaxLength();
			$options['MaxLength'] = $length > 0 ? $length : '';
		}

		if($this->hasEventHandler('OnLoadingText'))
			$options['LoadTextOnEdit'] = true;
			
		$options['ReadOnly']=$this->getReadOnly();
		return $options;
	}

	/**
	 * Raised when editing the content is requsted to be loaded from the
	 * server side.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onLoadingText($param)
	{
		$this->raiseEvent('OnLoadingText',$this,$param);
	}

	/**
	 * @return string corresponding javascript class name for this TInPlaceTextBox
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TInPlaceTextBox';
	}

	/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control.
	 */
	protected function addAttributesToRender($writer)
	{
		//calls the TWebControl to avoid rendering other attribute normally render for a textbox.
		TWebControl::addAttributesToRender($writer);
		$writer->addAttribute('id',$this->getLabelClientID());
		$this->getActiveControl()->registerCallbackClientScript(
			$this->getClientClassName(), $this->getPostBackOptions());
	}

}
