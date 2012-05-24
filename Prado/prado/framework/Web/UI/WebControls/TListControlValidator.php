<?php
/**
 * TListControlValidator class file
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TListControlValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Using TBaseValidator class
 */
Prado::using('System.Web.UI.WebControls.TBaseValidator');

/**
 * TListControlValidator class.
 *
 * TListControlValidator checks the number of selection and their values
 * for a <b>TListControl that allows multiple selection</b>.
 *
 * You can specify the minimum or maximum (or both) number of selections
 * required using the {@link setMinSelection MinSelection} and
 * {@link setMaxSelection MaxSelection} properties, respectively. In addition,
 * you can specify a comma separated list of required selected values via the
 * {@link setRequiredSelections RequiredSelections} property.
 *
 * Examples
 * - At least two selections
 * <code>
 *	<com:TListBox ID="listbox" SelectionMode="Multiple">
 *		<com:TListItem Text="item1" Value="value1" />
 *		<com:TListItem Text="item2" Value="value2" />
 *		<com:TListItem Text="item3" Value="value3" />
 *	</com:TListBox>
 *
 *	<com:TListControlValidator
 *		ControlToValidate="listbox"
 *		MinSelection="2"
 *		ErrorMessage="Please select at least 2" />
 * </code>
 * - "value1" must be selected <b>and</b> at least 1 other
 * <code>
 *	<com:TCheckBoxList ID="checkboxes">
 *		<com:TListItem Text="item1" Value="value1" />
 *		<com:TListItem Text="item2" Value="value2" />
 *		<com:TListItem Text="item3" Value="value3" />
 *	</com:TCheckBoxList>
 *
 *	<com:TListControlValidator
 *		ControlToValidate="checkboxes"
 *		RequiredSelections="value1"
 *		MinSelection="2"
 *		ErrorMessage="Please select 'item1' and at least 1 other" />
 * </code>
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail.com>
 * @version $Id: TListControlValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TListControlValidator extends TBaseValidator
{
	/**
	 * Gets the name of the javascript class responsible for performing validation for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TListControlValidator';
	}

	/**
	 * @return integer min number of selections. Defaults to -1, meaning not set.
	 */
	public function getMinSelection()
	{
		return $this->getViewState('MinSelection',-1);
	}

	/**
	 * @param integer minimum number of selections.
	 */
	public function setMinSelection($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			$value=-1;
		$this->setViewState('MinSelection',$value,-1);
	}

	/**
	 * @return integer max number of selections.  Defaults to -1, meaning not set.
	 */
	public function getMaxSelection()
	{
		return $this->getViewState('MaxSelection',-1);
	}

	/**
	 * @param integer max number of selections.
	 */
	public function setMaxSelection($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			$value=-1;
		$this->setViewState('MaxSelection',$value,-1);
	}

	/**
	 * Get a comma separated list of required selected values.
	 * @return string comma separated list of required values.
	 */
	public function getRequiredSelections()
	{
		return $this->getViewState('RequiredSelections','');
	}

	/**
	 * Set the list of required values, using aa comma separated list.
	 * @param string comma separated list of required values.
	 */
	public function setRequiredSelections($value)
	{
		$this->setViewState('RequiredSelections',$value,'');
	}

	/**
	 * This method overrides the parent's implementation.
	 * The validation succeeds if the input component changes its data
	 * from the InitialValue or the input component is not given.
	 * @return boolean whether the validation succeeds
	 */
	protected function evaluateIsValid()
	{
		$control=$this->getValidationTarget();

		$exists = true;
		$values = $this->getSelection($control);
		$count = count($values);
		$required = $this->getRequiredValues();

		//if required, check the values
		if(!empty($required))
		{
			if($count < count($required) )
				return false;
			foreach($required as $require)
				$exists = $exists && in_array($require, $values);
		}

		$min = $this->getMinSelection();
		$max = $this->getMaxSelection();

		if($min !== -1 && $max !== -1)
			return $exists && $count >= $min && $count <= $max;
		else if($min === -1 && $max !== -1)
			return $exists && $count <= $max;
		else if($min !== -1 && $max === -1)
			return $exists && $count >= $min;
		else
			return $exists;
	}

	/**
	 * @param TListControl control to validate
	 * @return array number of selected values and its values.
	 */
	protected function getSelection($control)
	{
		$values = array();

		//get the data
		foreach($control->getItems() as $item)
		{
			if($item->getSelected())
				$values[] = $item->getValue();
		}
		return $values;
	}

	/**
	 * @return array list of required values.
	 */
	protected function getRequiredValues()
	{
		$required = array();
		$string = $this->getRequiredSelections();
		if(!empty($string))
			$required = preg_split('/,\s*/', $string);
		return $required;
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	protected function getClientScriptOptions()
	{
		$options = parent::getClientScriptOptions();
		$control = $this->getValidationTarget();

		if(!$control instanceof TListControl)
		{
			throw new TConfigurationException(
				'listcontrolvalidator_invalid_control',
				$this->getID(),$this->getControlToValidate(), get_class($control));
		}

		$min = $this->getMinSelection();
		$max = $this->getMaxSelection();
		if($min !== -1)
			$options['Min']= $min;
		if($max !== -1)
			$options['Max']= $max;
		$required = $this->getRequiredSelections();
		if(strlen($required) > 0)
			$options['Required']= $required;
		$options['TotalItems'] = $control->getItemCount();

		return $options;
	}
}
