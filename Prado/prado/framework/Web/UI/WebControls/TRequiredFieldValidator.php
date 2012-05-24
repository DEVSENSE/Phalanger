<?php
/**
 * TRequiredFieldValidator class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TRequiredFieldValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Using TBaseValidator class
 */
Prado::using('System.Web.UI.WebControls.TBaseValidator');

/**
 * TRequiredFieldValidator class
 *
 * TRequiredFieldValidator makes the associated input control a required field.
 * The input control fails validation if its value does not change from
 * the {@link setInitialValue InitialValue} property upon losing focus.
 *
 * Validation will also succeed if input is of TListControl type and the number
 * of selected values different from the initial value is greater than zero.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRequiredFieldValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRequiredFieldValidator extends TBaseValidator
{
	/**
	 * Gets the name of the javascript class responsible for performing validation for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TRequiredFieldValidator';
	}

	/**
	 * @return string the initial value of the associated input control. Defaults to empty string.
	 * If the associated input control does not change from this initial value
	 * upon postback, the validation fails.
	 */
	public function getInitialValue()
	{
		return $this->getViewState('InitialValue','');
	}

	/**
	 * @param string the initial value of the associated input control.
	 * If the associated input control does not change from this initial value
	 * upon postback, the validation fails.
	 */
	public function setInitialValue($value)
	{
		$this->setViewState('InitialValue',TPropertyValue::ensureString($value),'');
	}

	/**
	 * This method overrides the parent's implementation.
	 * The validation succeeds if the input component changes its data
	 * from the {@link getInitialValue InitialValue} or the input control is not given.
	 *
	 * Validation will also succeed if input is of TListControl type and the
	 * number of selected values different from the initial value is greater
	 * than zero.
	 *
	 * @return boolean whether the validation succeeds
	 */
	protected function evaluateIsValid()
	{
		$control = $this->getValidationTarget();
		if($control instanceof TListControl)
			return $this->validateListControl($control);
		else if($control instanceof TRadioButton && strlen($control->getGroupName()) > 0)
			return $this->validateRadioButtonGroup($control);
		else
			return $this->validateStandardControl($control);
	}
	
	private function validateListControl($control)
	{
		$initial = trim($this->getInitialValue());
		$count = 0;
		foreach($control->getItems() as $item)
		{
			if($item->getSelected() && $item->getValue() != $initial)
				$count++;
		}
		return $count > 0;
	}
	
	private function validateRadioButtonGroup($control)
	{
		$initial = trim($this->getInitialValue());
		foreach($control->getRadioButtonsInGroup() as $radio)
		{
			if($radio->getChecked())
			{
				if(strlen($value = $radio->getValue()) > 0)
					return $value !== $initial;
				else
					return true;
			}
		}
		return false;
	}
	
	private function validateStandardControl($control)
	{
		$initial = trim($this->getInitialValue());
		$value=$this->getValidationValue($control);
		return (is_bool($value) && $value) || trim($value)!==$initial;
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	protected function getClientScriptOptions()
	{
		$options = parent::getClientScriptOptions();
		$options['InitialValue']=$this->getInitialValue();
		$control = $this->getValidationTarget();
		if($control instanceof TListControl)
			$options['TotalItems'] = $control->getItemCount();
		if($control instanceof TRadioButton && strlen($control->getGroupName()) > 0)
			$options['GroupName'] = $control->getGroupName();
		return $options;
	}
}

