<?php
/**
 * TCustomValidator class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TCustomValidator.php 2630 2009-04-04 09:53:57Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 */

/**
 * Using TBaseValidator class
 */
Prado::using('System.Web.UI.WebControls.TBaseValidator');

/**
 * TCustomValidator class
 *
 * TCustomValidator performs user-defined validation (either
 * server-side or client-side or both) on an input component.
 *
 * To create a server-side validation function, provide a handler for
 * the {@link onServerValidate OnServerValidate} event that performs the validation.
 * The data string of the input control to validate can be accessed
 * by {@link TServerValidateEventParameter::getValue Value} of the event parameter.
 * The result of the validation should be stored in the
 * {@link TServerValidateEventParameter::getIsValid IsValid} property of the event
 * parameter.
 *
 * To create a client-side validation function, add the client-side
 * validation javascript function to the page template.
 * The function should have the following signature:
 * <code>
 * <script type="text/javascript"><!--
 * function ValidationFunctionName(sender, parameter)
 * {
 *    // if(parameter == ...)
 *    //    return true;
 *    // else
 *    //    return false;
 * }
 * --></script>
 * </code>
 * Use the {@link setClientValidationFunction ClientValidationFunction} property
 * to specify the name of the client-side validation script function associated
 * with the TCustomValidator.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCustomValidator.php 2630 2009-04-04 09:53:57Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TCustomValidator extends TBaseValidator
{
	/**
	 * Gets the name of the javascript class responsible for performing validation for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TCustomValidator';
	}

	/**
	 * @return string the name of the custom client-side script function used for validation.
	 */
	public function getClientValidationFunction()
	{
		return $this->getViewState('ClientValidationFunction','');
	}

	/**
	 * Sets the name of the custom client-side script function used for validation.
	 * @param string the script function name
	 */
	public function setClientValidationFunction($value)
	{
		$this->setViewState('ClientValidationFunction',$value,'');
	}

	/**
	 * This method overrides the parent's implementation.
	 * The validation succeeds if {@link onServerValidate} returns true.
	 * @return boolean whether the validation succeeds
	 */
	public function evaluateIsValid()
	{
		$value = '';
		if($this->getValidationTarget()!==null)
			$value=$this->getValidationValue($this->getValidationTarget());
		return $this->onServerValidate($value);
	}

	/**
	 * This method is invoked when the server side validation happens.
	 * It will raise the <b>OnServerValidate</b> event.
	 * The method also allows derived classes to handle the event without attaching a delegate.
	 * <b>Note</b> The derived classes should call parent implementation
	 * to ensure the <b>OnServerValidate</b> event is raised.
	 * @param string the value to be validated
	 * @return boolean whether the value is valid
	 */
	public function onServerValidate($value)
	{
		$param=new TServerValidateEventParameter($value,true);
		$this->raiseEvent('OnServerValidate',$this,$param);
		if($this->getValidationTarget()==null)
			return true;
		else
			return $param->getIsValid();
	}

	/**
	 * @return TControl control to be validated. Null if no control is found.
	 */
	public function getValidationTarget()
	{
		if(($id=$this->getControlToValidate())!=='' && ($control=$this->findControl($id))!==null)
			return $control;
		else if(($id=$this->getControlToValidate())!=='')
			throw new TInvalidDataTypeException('basevalidator_validatable_required',get_class($this));
		else
			return null;
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	protected function getClientScriptOptions()
	{
		$options=parent::getClientScriptOptions();
		if(($clientJs=$this->getClientValidationFunction())!=='')
			$options['ClientValidationFunction']=$clientJs;
		return $options;
	}

	/**
	 * Only register the client-side validator if
	 * {@link setClientValidationFunction ClientValidationFunction} is set.
	 */
	protected function registerClientScriptValidator()
	{
		if($this->getClientValidationFunction()!=='')
			parent::registerClientScriptValidator();
	}
}

/**
 * TServerValidateEventParameter class
 *
 * TServerValidateEventParameter encapsulates the parameter data for
 * <b>OnServerValidate</b> event of TCustomValidator components.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCustomValidator.php 2630 2009-04-04 09:53:57Z godzilla80@gmx.net $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TServerValidateEventParameter extends TEventParameter
{
	/**
	 * the value to be validated
	 * @var string
	 */
	private $_value='';
	/**
	 * whether the value is valid
	 * @var boolean
	 */
	private $_isValid=true;

	/**
	 * Constructor.
	 * @param string property value to be validated
	 * @param boolean whether the value is valid
	 */
	public function __construct($value,$isValid)
	{
		$this->_value=$value;
		$this->setIsValid($isValid);
	}

	/**
	 * @return string value to be validated
	 */
	public function getValue()
	{
		return $this->_value;
	}

	/**
	 * @return boolean whether the value is valid
	 */
	public function getIsValid()
	{
		return $this->_isValid;
	}

	/**
	 * @param boolean whether the value is valid
	 */
	public function setIsValid($value)
	{
		$this->_isValid=TPropertyValue::ensureBoolean($value);
	}
}
