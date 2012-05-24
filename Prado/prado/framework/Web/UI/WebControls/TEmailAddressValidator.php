<?php
/**
 * TEmailAddressValidator class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TEmailAddressValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Using TRegularExpressionValidator class
 */
Prado::using('System.Web.UI.WebControls.TRegularExpressionValidator');

/**
 * TEmailAddressValidator class
 *
 * TEmailAddressValidator validates whether the value of an associated
 * input component is a valid email address. If {@link getCheckMXRecord CheckMXRecord}
 * is true, it will check MX record for the email adress, provided
 * checkdnsrr() is available in the installed PHP.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TEmailAddressValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TEmailAddressValidator extends TRegularExpressionValidator
{
	/**
	 * Regular expression used to validate the email address
	 */
	const EMAIL_REGEXP="\\w+([-+.]\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*";

	/**
	 * Gets the name of the javascript class responsible for performing validation for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TEmailAddressValidator';
	}

	/**
	 * @return string the regular expression that determines the pattern used to validate a field.
	 */
	public function getRegularExpression()
	{
		$regex=parent::getRegularExpression();
		return $regex===''?self::EMAIL_REGEXP:$regex;
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	public function evaluateIsValid()
	{
		$valid=parent::evaluateIsValid();
		if($valid && $this->getCheckMXRecord() && function_exists('checkdnsrr'))
		{
			if(($value=$this->getValidationValue($this->getValidationTarget()))!=='')
			{
				if(($pos=strpos($value,'@'))!==false)
				{
					$domain=substr($value,$pos+1);
					return $domain===''?false:checkdnsrr($domain,'MX');
				}
				else
					return false;
			}
		}
		return $valid;
	}

	/**
	 * @return boolean whether to check MX record for the email address being validated. Defaults to true.
	 */
	public function getCheckMXRecord()
	{
		return $this->getViewState('CheckMXRecord',true);
	}

	/**
	 * @param boolean whether to check MX record for the email address being validated.
	 * Note, if {@link checkdnsrr} is not available, this check will not be performed.
	 */
	public function setCheckMXRecord($value)
	{
		$this->setViewState('CheckMXRecord',TPropertyValue::ensureBoolean($value),true);
	}
}

