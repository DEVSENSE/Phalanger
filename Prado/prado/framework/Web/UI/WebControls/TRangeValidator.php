<?php
/**
 * TRangeValidator class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TRangeValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Using TBaseValidator class
 */
Prado::using('System.Web.UI.WebControls.TBaseValidator');

/**
 * TRangeValidator class
 *
 * TRangeValidator tests whether an input value is within a specified range.
 *
 * TRangeValidator uses three key properties to perform its validation.
 * The {@link setMinValue MinValue} and {@link setMaxValue MaxValue}
 * properties specify the minimum and maximum values of the valid range.
 * The {@link setDataType DataType} property is used to specify the
 * data type of the value and the minimum and maximum range values.
 * These values are converted to this data type before the validation
 * operation is performed. The following value types are supported:
 * - <b>Integer</b> A 32-bit signed integer data type.
 * - <b>Float</b> A double-precision floating point number data type.
 * - <b>Date</b> A date data type. The date format can be specified by
 *   setting {@link setDateFormat DateFormat} property, which must be recognizable
 *   by {@link TSimpleDateFormatter}. If the property is not set,
 *   the GNU date syntax is assumed.
 * - <b>String</b> A string data type.
 * - <b>StringLength</b> check for string length.
 *
 * If {@link setStrictComparison StrictComparison} is true, then the ranges
 * are compared as strictly less than the max value and/or strictly greater than the min value.
 *
 * The TRangeValidator allows a special DataType "StringLength" that
 * can be used to verify minimum and maximum string length. The
 * {@link setCharset Charset} property can be used to force a particular
 * charset for comparison. Otherwise, the application charset is used and is
 * defaulted as UTF-8.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRangeValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRangeValidator extends TBaseValidator
{
	/**
	 * Gets the name of the javascript class responsible for performing validation for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TRangeValidator';
	}

	/**
	 * @return string the minimum value of the validation range.
	 */
	public function getMinValue()
	{
		return $this->getViewState('MinValue','');
	}

	/**
	 * Sets the minimum value of the validation range.
	 * @param string the minimum value
	 */
	public function setMinValue($value)
	{
		$this->setViewState('MinValue',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return string the maximum value of the validation range.
	 */
	public function getMaxValue()
	{
		return $this->getViewState('MaxValue','');
	}

	/**
	 * Sets the maximum value of the validation range.
	 * @param string the maximum value
	 */
	public function setMaxValue($value)
	{
		$this->setViewState('MaxValue',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @param boolean true to perform strict comparison (i.e. strictly less than max and/or strictly greater than min).
	 */
	public function setStrictComparison($value)
	{
		$this->setViewState('StrictComparison', TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * @return boolean true to perform strict comparison.
	 */
	public function getStrictComparison()
	{
		return $this->getViewState('StrictComparison', false);
	}

	/**
	 * @return TRangeValidationDataType the data type that the values being compared are
	 * converted to before the comparison is made. Defaults to TRangeValidationDataType::String.
	 */
	public function getDataType()
	{
		return $this->getViewState('DataType',TRangeValidationDataType::String);
	}

	/**
	 * Sets the data type that the values being compared are converted to before the comparison is made.
	 * @param TRangeValidationDataType the data type
	 */
	public function setDataType($value)
	{
		$this->setViewState('DataType',TPropertyValue::ensureEnum($value,'TRangeValidationDataType'),TRangeValidationDataType::String);
	}

	/**
     * Sets the date format for a date validation
     * @param string the date format value
     */
	public function setDateFormat($value)
	{
		$this->setViewState('DateFormat', $value, '');
	}

	/**
	 * @return string the date validation date format if any
	 */
	public function getDateFormat()
	{
		return $this->getViewState('DateFormat', '');
	}

	/**
	 * @param string charset for string length comparison.
	 */
	public function setCharset($value)
	{
		$this->setViewState('Charset', $value, '');
	}

	/**
	 * @return string charset for string length comparison.
	 */
	public function getCharset()
	{
		return $this->getViewState('Charset', '');
	}

	/**
	 * This method overrides the parent's implementation.
	 * The validation succeeds if the input data is within the range.
	 * The validation always succeeds if the input data is empty.
	 * @return boolean whether the validation succeeds
	 */
	protected function evaluateIsValid()
	{
		$value=$this->getValidationValue($this->getValidationTarget());
		if($value==='')
			return true;

		switch($this->getDataType())
		{
			case TRangeValidationDataType::Integer:
				return $this->isValidInteger($value);
			case TRangeValidationDataType::Float:
				return $this->isValidFloat($value);
			case TRangeValidationDataType::Date:
				return $this->isValidDate($value);
			case TRangeValidationDataType::StringLength:
				return $this->isValidStringLength($value);
			default:
				return $this->isValidString($value);
		}
	}

	/**
	* Determine if the value is within the integer range.
	* @param string value to validate true
	* @return boolean true if within integer range.
	*/
	protected function isValidInteger($value)
	{
		$minValue=$this->getMinValue();
		$maxValue=$this->getMaxValue();

		$valid=preg_match('/^[-+]?[0-9]+$/',trim($value));
		$value=intval($value);
		if($minValue!=='')
			$valid=$valid && $this->isGreaterThan($value, intval($minValue));
		if($maxValue!=='')
			$valid=$valid && $this->isLessThan($value,intval($maxValue));
		return $valid;
	}

	protected function isLessThan($left,$right)
	{
		return $this->getStrictComparison() ? $left < $right : $left <= $right;
	}

	protected function isGreaterThan($left, $right)
	{
		return $this->getStrictComparison() ? $left > $right : $left >= $right;
	}

	/**
	 * Determine if the value is within the specified float range.
	 * @param string value to validate
	 * @return boolean true if within range.
	 */
	protected function isValidFloat($value)
	{
		$minValue=$this->getMinValue();
		$maxValue=$this->getMaxValue();

		$valid=preg_match('/^[-+]?([0-9]*\.)?[0-9]+([eE][-+]?[0-9]+)?$/',trim($value));
		$value=floatval($value);
		if($minValue!=='')
			$valid=$valid && $this->isGreaterThan($value,floatval($minValue));
		if($maxValue!=='')
			$valid=$valid && $this->isLessThan($value,floatval($maxValue));
		return $valid;
	}

	/**
	 * Determine if the date is within the specified range.
	 * Uses pradoParseDate and strtotime to get the date from string.
	 * @param string date as string to validate
	 * @return boolean true if within range.
	 */
	protected function isValidDate($value)
	{
		$minValue=$this->getMinValue();
		$maxValue=$this->getMaxValue();

		$valid=true;

		$dateFormat = $this->getDateFormat();
		if($dateFormat!=='')
		{
			$formatter=Prado::createComponent('System.Util.TSimpleDateFormatter', $dateFormat);
			$value = $formatter->parse($value, $dateFormat);
			if($minValue!=='')
				$valid=$valid && $this->isGreaterThan($value,$formatter->parse($minValue));
			if($maxValue!=='')
				$valid=$valid && $this->isLessThan($value,$formatter->parse($maxValue));
			return $valid;
		}
		else
		{
			$value=strtotime($value);
			if($minValue!=='')
				$valid=$valid && $this->isGreaterThan($value,strtotime($minValue));
			if($maxValue!=='')
				$valid=$valid && $this->isLessThan($value,strtotime($maxValue));
			return $valid;
		}
	}

	/**
	 * Compare the string with a minimum and a maxiumum value.
	 * Uses strcmp for comparision.
	 * @param string value to compare with.
	 * @return boolean true if the string is within range.
	 */
	protected function isValidString($value)
	{
		$minValue=$this->getMinValue();
		$maxValue=$this->getMaxValue();

		$valid=true;
		if($minValue!=='')
			$valid=$valid && $this->isGreaterThan(strcmp($value,$minValue),0);
		if($maxValue!=='')
			$valid=$valid && $this->isLessThan(strcmp($value,$maxValue),0);
		return $valid;
	}

	/**
	 * @param string string for comparision
	 * @return boolean true if min and max string length are satisfied.
	 */
	protected function isValidStringLength($value)
	{
		$minValue=$this->getMinValue();
		$maxValue=$this->getMaxValue();

		$valid=true;
		$charset = $this->getCharset();
		if($charset==='')
		{
			$app= $this->getApplication()->getGlobalization();
			$charset = $app ? $app->getCharset() : null;
			if(!$charset)
				$charset = 'UTF-8';
		}

		$length = iconv_strlen($value, $charset);
		if($minValue!=='')
			$valid = $valid && $this->isGreaterThan($length,intval($minValue));
		if($maxValue!=='')
			$valid = $valid && $this->isLessThan($length,intval($maxValue));
		return $valid;
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	protected function getClientScriptOptions()
	{
		$options=parent::getClientScriptOptions();
		$options['MinValue']=$this->getMinValue();
		$options['MaxValue']=$this->getMaxValue();
		$options['DataType']=$this->getDataType();
		$options['StrictComparison']=$this->getStrictComparison();
		if(($dateFormat=$this->getDateFormat())!=='')
			$options['DateFormat']=$dateFormat;
		return $options;
	}
}


/**
 * TRangeValidationDataType class.
 * TRangeValidationDataType defines the enumerable type for the possible data types that
 * a range validator can validate upon.
 *
 * The following enumerable values are defined:
 * - Integer
 * - Float
 * - Date
 * - String
 * - StringLength
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRangeValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TRangeValidationDataType extends TValidationDataType
{
	const StringLength='StringLength';
}
