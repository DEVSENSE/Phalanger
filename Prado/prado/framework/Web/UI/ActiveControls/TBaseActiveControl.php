<?php
/**
 * TBaseActiveControl and TBaseActiveCallbackControl class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TBaseActiveControl.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */

Prado::using('System.Web.UI.ActiveControls.TCallbackClientSide');

/**
 * TBaseActiveControl class provided additional basic property for every
 * active control. An instance of TBaseActiveControl or its decendent
 * TBaseActiveCallbackControl is created by {@link TActiveControlAdapter::getBaseActiveControl()}
 * method.
 *
 * The {@link setEnableUpdate EnableUpdate} property determines wether the active
 * control is allowed to update the contents of the client-side when the callback
 * response returns.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @version $Id: TBaseActiveControl.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TBaseActiveControl extends TComponent
{
	/**
	 * @var TMap map of active control options.
	 */
	private $_options;
	/**
	 * @var TControl attached control.
	 */
	private $_control;

	/**
	 * Constructor. Attach a base active control to an active control instance.
	 * @param TControl active control
	 */
	public function __construct($control)
	{
		$this->_control = $control;
		$this->_options = new TMap;
	}

	/**
	 * Sets a named options with a value. Options are used to store and retrive
	 * named values for the base active controls.
	 * @param string option name.
	 * @param mixed new value.
	 * @param mixed default value.
	 * @return mixed options value.
	 */
	protected function setOption($name,$value,$default=null)
	{
		$value = ($value===null) ? $default : $value;
		if($value!==null)
			$this->_options->add($name,$value);
	}

	/**
	 * Gets an option named value. Options are used to store and retrive
	 * named values for the base active controls.
	 * @param string option name.
	 * @param mixed default value.
	 * @return mixed options value.
	 */
	protected function getOption($name,$default=null)
	{
		$item = $this->_options->itemAt($name);
		return ($item===null) ? $default : $item;
	}

	/**
	 * @return TMap active control options
	 */
	protected function getOptions()
	{
		return $this->_options;
	}

	/**
	 * @return TPage the page containing the attached control.
	 */
	protected function getPage()
	{
		return $this->_control->getPage();
	}

	/**
	 * @return TControl the attached control.
	 */
	protected function getControl()
	{
		return $this->_control;
	}

	/**
	 * @param boolean true to allow fine grain callback updates.
	 */
	public function setEnableUpdate($value)
	{
		$this->setOption('EnableUpdate', TPropertyValue::ensureBoolean($value), true);
	}

	/**
	 * @return boolean true to allow fine grain callback updates.
	 */
	public function getEnableUpdate()
	{
		return $this->getOption('EnableUpdate', true);
	}

	/**
	 * Returns true if callback response is allowed to update the browser contents.
	 * Is is true if the control is initilized, and is a callback request and
	 * the {@link setEnableUpdate EnabledUpdate} property is true and
	 * the page is not loading post data.
	 * @return boolean true if the callback response is allowed update
	 * client-side contents.
	 */
	public function canUpdateClientSide()
	{
		return 	$this->getControl()->getHasChildInitialized()
				&& $this->getPage()->getIsLoadingPostData() == false
				&& $this->getPage()->getIsCallback()
				&& $this->getEnableUpdate();
	}
}

/**
 * TBaseActiveCallbackControl is a common set of options and functionality for
 * active controls that can perform callback requests.
 *
 * The properties of TBaseActiveCallbackControl can be accessed and changed from
 * each individual active controls' {@link getActiveControl ActiveControl}
 * property.
 *
 * The following example to set the validation group property of a TCallback component.
 * <code>
 * 	<com:TCallback ActiveControl.ValidationGroup="group1" ... />
 * </code>
 *
 * Additional client-side options and events can be set using the
 * {@link getClientSide ClientSide} property. The following example to show
 * an alert box when a TCallback component response returns successfully.
 * <code>
 * 	<com:TCallback Active.Control.ClientSide.OnSuccess="alert('ok!')" ... />
 * </code>
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TBaseActiveControl.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TBaseActiveCallbackControl extends TBaseActiveControl
{
	/**
	 * Callback client-side options can be set by setting the properties of
	 * the ClientSide property. E.g. <com:TCallback ActiveControl.ClientSide.OnSuccess="..." />
	 * See {@link TCallbackClientSide} for details on the properties of ClientSide.
	 * @return TCallbackClientSide client-side callback options.
	 */
	public function getClientSide()
	{
		if(($client = $this->getOption('ClientSide'))===null)
		{
			$client = $this->createClientSide();
			$this->setOption('ClientSide', $client);
		}
		return $client;
	}

	/**
	 * Sets the client side options. Can only be set when client side is null.
	 * @param TCallbackClientSide client side options.
	 */
	public function setClientSide($client)
	{
		if( $this->getOption('ClientSide')===null)
			$this->setOption('ClientSide', $client);
		else
			throw new TConfigurationException(
				'active_controls_client_side_exists', $this->getControl()->getID());
	}

	/**
	 * @return TCallbackClientSide callback client-side options.
	 */
	protected function createClientSide()
	{
		return new TCallbackClientSide;
	}

	/**
	 * Sets default callback options. Takes the ID of a TCallbackOptions
	 * component to duplicate the client-side
	 * options for this control. The {@link getClientSide ClientSide}
	 * subproperties has precendent over the CallbackOptions property.
	 * @param string ID of a TCallbackOptions control from which ClientSide
	 * options are cloned.
	 */
	public function setCallbackOptions($value)
	{
		$this->setOption('CallbackOptions', $value, '');
	}

	/**
	 * @return string ID of a TCallbackOptions control from which ClientSide
	 * options are duplicated.
	 */
	public function getCallbackOptions()
	{
		return $this->getOption('CallbackOptions', '');
	}

	/**
	 * Returns an array of default callback client-side options. The default options
	 * are obtained from the client-side options of a TCallbackOptions control with
	 * ID specified by {@link setCallbackOptionsID CallbackOptionsID}.
	 * @return array list of default callback client-side options.
	 */
	protected function getDefaultClientSideOptions()
	{
		if(($id=$this->getCallbackOptions())!=='')
		{
			if(($pos=strrpos($id,'.'))!==false)
			{
				$control=$this->getControl()->getSubProperty(substr($id,0,$pos));
				$newid=substr($id,$pos+1);
				if ($control!==null)
					$control=$control->findControl($newid);
			}
			else
			{
				 $control=$this->getControl()->findControl($id);
			}

			if($control instanceof TCallbackOptions)
				return $control->getClientSide()->getOptions()->toArray();
			else
				throw new TConfigurationException('callback_invalid_callback_options_ID', $id);
		}

		return array();
	}

	/**
	 * @return boolean whether callback event trigger by this button will cause
	 * input validation, default is true
	 */
	public function getCausesValidation()
	{
		return $this->getOption('CausesValidation',true);
	}

	/**
	 * @param boolean whether callback event trigger by this button will cause
	 * input validation
	 */
	public function setCausesValidation($value)
	{
		$this->setOption('CausesValidation',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @return string the group of validators which the button causes validation
	 * upon callback
	 */
	public function getValidationGroup()
	{
		return $this->getOption('ValidationGroup','');
	}

	/**
	 * @param string the group of validators which the button causes validation
	 * upon callback
	 */
	public function setValidationGroup($value)
	{
		$this->setOption('ValidationGroup',$value,'');
	}

	/**
	 * @return boolean whether to perform validation if the callback is
	 * requested.
	 */
	public function canCauseValidation()
	{
		if($this->getCausesValidation())
		{
			$group=$this->getValidationGroup();
			return $this->getPage()->getValidators($group)->getCount()>0;
		}
		else
			return false;
	}

	/**
	 * @param mixed callback parameter value.
	 */
	public function setCallbackParameter($value)
	{
		$this->setOption('CallbackParameter', $value, '');
	}

	/**
	 * @return mixed callback parameter value.
	 */
	public function getCallbackParameter()
	{
		return $this->getOption('CallbackParameter', '');
	}


	/**
	 * @return array list of callback javascript options.
	 */
	protected function getClientSideOptions()
	{
		$default = $this->getDefaultClientSideOptions();
		$options = array_merge($default,$this->getClientSide()->getOptions()->toArray());
		$validate = $this->getCausesValidation();
		$options['CausesValidation']= $validate ? '' : false;
		$options['ValidationGroup']=$this->getValidationGroup();
		$options['CallbackParameter'] = $this->getCallbackParameter();
		return $options;
	}

	/**
	 * Registers the callback control javascript code. Client-side options are
	 * merged and passed to the javascript code. This method should be called by
	 * Active component developers wanting to register the javascript to initialize
	 * the active component with additional options offered by the
	 * {@link getClientSide ClientSide} property.
	 * @param string client side javascript class name.
	 * @param array additional callback options.
	 */
	public function registerCallbackClientScript($class,$options=null)
	{
		$cs = $this->getPage()->getClientScript();
		if(is_array($options))
			$options = array_merge($this->getClientSideOptions(),$options);
		else
			$options = $this->getClientSideOptions();

		//remove true as default to save bytes
		if($options['CausesValidation']===true)
			$options['CausesValidation']='';
		$cs->registerCallbackControl($class, $options);
	}

	/**
	 * Returns the javascript callback request instance. To invoke a callback
	 * request for this control call the <tt>dispatch()</tt> method on the
	 * request instance. Example code in javascript
	 * <code>
	 *   var request = <%= $this->mycallback->ActiveControl->Javascript %>;
	 *   request.setParameter('hello');
	 *   request.dispatch(); //make the callback request.
	 * </code>
	 *
	 * Alternatively,
	 * <code>
	 * //dispatches immediately
	 * Prado.Callback("<%= $this->mycallback->UniqueID %>",
	 *    $this->mycallback->ActiveControl->JsCallbackOptions);
	 * </code>
	 * @return string javascript client-side callback request object (javascript
	 * code)
	 */
	public function getJavascript()
	{
		$client = $this->getPage()->getClientScript();
		return $client->getCallbackReference($this->getControl(),$this->getClientSideOptions());
	}

	/**
	 * @param string callback requestion options as javascript code.
	 */
	public function getJsCallbackOptions()
	{
		return TJavaScript::encode($this->getClientSideOptions());
	}
}

