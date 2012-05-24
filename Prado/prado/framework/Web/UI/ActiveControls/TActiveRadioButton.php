<?php
/**
 * TActiveRadioButton class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveRadioButton.php 2600 2009-01-07 12:58:53Z christophe.boulain $
 * @package System.Web.UI.ActiveControls
 */

/**
 * TActiveRadioButton class.
 *
 * The active control counter part to radio button. The {@link setAutoPostBack AutoPostBack}
 * property is set to true by default. Thus, when the radio button is clicked a
 * {@link onCallback OnCallback} event is raise after {@link OnCheckedChanged} event.
 *
 * The {@link setText Text} and {@link setChecked Checked} properties can be
 * changed during a callback.
 *
 * The {@link setGroupName GroupName} property may <b>NOT</b> be changed
 * during callback because the client-side <tt>name</tt> attribute is read-only
 * and can not be changed using javascript.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveRadioButton.php 2600 2009-01-07 12:58:53Z christophe.boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveRadioButton extends TRadioButton implements IActiveControl, ICallbackEventHandler
{
	/**
	 * Creates a new callback control, sets the adapter to
	 * TActiveControlAdapter. If you override this class, be sure to set the
	 * adapter appropriately by, for example, by calling this constructor.
	 */
	public function __construct()
	{
		parent::__construct();
		$this->setAdapter(new TActiveControlAdapter($this));
		$this->setAutoPostBack(true);
	}

	/**
	 * @return TBaseActiveCallbackControl standard callback control options.
	 */
	public function getActiveControl()
	{
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * @return TCallbackClientSide client side request options.
	 */
	public function getClientSide()
	{
		return $this->getAdapter()->getBaseActiveControl()->getClientSide();
	}

	/**
	 * Raises the callback event. This method is required by {@link
	 * ICallbackEventHandler} interface.
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the event parameter
	 */
 	public function raiseCallbackEvent($param)
	{
		$this->onCallback($param);
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
		$this->raiseEvent('OnCallback', $this, $param);
	}

	/**
	 * Updates the button text on the client-side if the
	 * {@link setEnableUpdate EnableUpdate} property is set to true.
	 * @param string caption of the button
	 */
	public function setText($value)
	{
		parent::setText($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->update(
				$this->getDefaultLabelID(), $value);
	}

	/**
	 * Checks the radio button.
	 * Updates radio button checked state on the client-side if the
	 * {@link setEnableUpdate EnableUpdate} property is set to true.
	 * @param boolean whether the radio button is to be checked or not.
	 */
	public function setChecked($value)
	{
		$value = TPropertyValue::ensureBoolean($value);
		parent::setChecked($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->check($this, $value);
	}

	/**
	 * Override parent implementation, no javascript is rendered here instead
	 * the javascript required for active control is registered in {@link addAttributesToRender}.
	 */
	protected function renderClientControlScript($writer)
	{
	}

	/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control.
	 * Since 3.1.4, the javascript code is not rendered if {@link setAutoPostBack AutoPostBack} is false
	 * 
	 */
	protected function renderInputTag($writer,$clientID,$onclick)
	{
		parent::renderInputTag($writer,$clientID,$onclick);
		if ($this->getAutoPostBack())
			$this->getActiveControl()->registerCallbackClientScript(
				$this->getClientClassName(), $this->getPostBackOptions());
	}

	/**
	 * @return string corresponding javascript class name for this TActiveRadioButton.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TActiveRadioButton';
	}

	/**
	 * Overrides parent implementation to ensure label has ID.
	 * @return TMap list of attributes to be rendered for label beside the radio button
	 */
	public function getLabelAttributes()
	{
		$attributes = parent::getLabelAttributes();
		$attributes['id'] = $this->getDefaultLabelID();
		return $attributes;
	}

	/**
	 * Renders a label beside the radio button.
	 * @param THtmlWriter the writer for the rendering purpose
	 * @param string radio button id
	 * @param string label text
	 */
	protected function renderLabel($writer,$clientID,$text)
	{
		$writer->addAttribute('id', $this->getDefaultLabelID());
		parent::renderLabel($writer, $clientID, $text);
	}

	/**
	 * @return string radio button label ID;
	 */
	protected function getDefaultLabelID()
	{
		if($attributes=$this->getViewState('LabelAttributes',null))
			return $this->getLabelAttributes()->itemAt('id');
		else
			return $this->getClientID().'_label';
	}
}

?>
