<?php
/**
 * TRequiredFieldValidator class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TRegularExpressionValidator.php 2754 2010-01-14 11:29:33Z Christophe.Boulain $
 * @package System.Web.UI.WebControls
 */

/**
 * Using TBaseValidator class
 */
Prado::using('System.Web.UI.WebControls.TBaseValidator');

/**
 * TRegularExpressionValidator class
 *
 * TRegularExpressionValidator validates whether the value of an associated
 * input component matches the pattern specified by a regular expression.
 *
 * You can specify the regular expression by setting the {@link setRegularExpression RegularExpression}
 * property. Some commonly used regular expressions include:
 * <pre>
 * French Phone Number: (0( \d|\d ))?\d\d \d\d(\d \d| \d\d )\d\d
 * French Postal Code: \d{5}
 * German Phone Number: ((\(0\d\d\) |(\(0\d{3}\) )?\d )?\d\d \d\d \d\d|\(0\d{4}\) \d \d\d-\d\d?)
 * German Postal Code: (D-)?\d{5}
 * Email Address: \w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*
 * Japanese Phone Number: (0\d{1,4}-|\(0\d{1,4}\) ?)?\d{1,4}-\d{4}
 * Japanese Postal Code: \d{3}(-(\d{4}|\d{2}))?
 * P.R.C. Phone Number: (\(\d{3}\)|\d{3}-)?\d{8}
 * P.R.C. Postal Code: \d{6}
 * P.R.C. Social Security Number: \d{18}|\d{15}
 * U.S. Phone Number: ((\(\d{3}\) ?)|(\d{3}-))?\d{3}-\d{4}
 * U.S. ZIP Code: \d{5}(-\d{4})?
 * U.S. Social Security Number: \d{3}-\d{2}-\d{4}
 * </pre>
 *
 * Note, the validation succeeds if the associated input control contains empty input.
 * Use a {@link TRequiredFieldValidator} to ensure the input is not empty.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRegularExpressionValidator.php 2754 2010-01-14 11:29:33Z Christophe.Boulain $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRegularExpressionValidator extends TBaseValidator
{
	/**
	 * Gets the name of the javascript class responsible for performing validation for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TRegularExpressionValidator';
	}

	/**
	 * @return string the regular expression that determines the pattern used to validate a field.
	 */
	public function getRegularExpression()
	{
		return $this->getViewState('RegularExpression','');
	}

	/**
	 * @param string the regular expression that determines the pattern used to validate a field.
	 */
	public function setRegularExpression($value)
	{
		$this->setViewState('RegularExpression',$value,'');
	}

	/**
	 * This method overrides the parent's implementation.
	 * The validation succeeds if the input data matches the regular expression.
	 * The validation always succeeds if ControlToValidate is not specified
	 * or the regular expression is empty, or the input data is empty.
	 * @return boolean whether the validation succeeds
	 */
	public function evaluateIsValid()
	{
		if(($value=$this->getValidationValue($this->getValidationTarget()))==='')
			return true;
		if(($expression=addcslashes($this->getRegularExpression(),"/"))!=='')
		{
			$mods = $this->getPatternModifiers();
			return preg_match("/^$expression\$/{$mods}",$value);
		}
		else
			return true;
	}

	/**
	 * @param string pattern modifiers for server side validation,
	 * see http://www.php.net/manual/en/reference.pcre.pattern.modifiers.php
	 */
	public function setPatternModifiers($value)
	{
		$this->setViewState('PatternModifiers', $value);
	}

	/**
	 * @return string pattern modifiers, no modifiers by default.
	 */
	public function getPatternModifiers()
	{
		return $this->getViewState('PatternModifiers', '');
	}

	/**
	 * @param string pattern modifiers for clientside.
	 * (Only 'g','i' and 'm' are available.)
	 */
	public function setClientSidePatternModifiers($value)
	{
		$this->setViewState('ClientSidePatternModifiers', $value);
	}

	/**
	 * @return string clientside pattern modifiers, no modifiers by default.
	 */
	public function getClientSidePatternModifiers()
	{
		return $this->getViewState('ClientSidePatternModifiers', '');
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	protected function getClientScriptOptions()
	{
		$options = parent::getClientScriptOptions();
		$options['ValidationExpression']=$this->getRegularExpression();
		$options['PatternModifiers']=$this->getClientSidePatternModifiers();
		return $options;
	}
}

