<?php
/**
 * TCaptchaValidator class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TCaptchaValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.Web.UI.WebControls.TBaseValidator');
Prado::using('System.Web.UI.WebControls.TCaptcha');

/**
 * TCaptchaValidator class
 *
 * TCaptchaValidator validates user input against a CAPTCHA represented by
 * a {@link TCaptcha} control. The input control fails validation if its value
 * is not the same as the token displayed in CAPTCHA. Note, if the user does
 * not enter any thing, it is still considered as failing the validation.
 *
 * To use TCaptchaValidator, specify the {@link setControlToValidate ControlToValidate}
 * to be the ID path of the input control (usually a {@link TTextBox} control}.
 * Also specify the {@link setCaptchaControl CaptchaControl} to be the ID path of
 * the CAPTCHA control that the user input should be compared with.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCaptchaValidator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TCaptchaValidator extends TBaseValidator
{
	/**
	 * Gets the name of the javascript class responsible for performing validation for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TCaptchaValidator';
	}

	/**
	 * @return string the ID path of the CAPTCHA control to validate
	 */
	public function getCaptchaControl()
	{
		return $this->getViewState('CaptchaControl','');
	}

	/**
	 * Sets the ID path of the CAPTCHA control to validate.
	 * The ID path is the dot-connected IDs of the controls reaching from
	 * the validator's naming container to the target control.
	 * @param string the ID path
	 */
	public function setCaptchaControl($value)
	{
		$this->setViewState('CaptchaControl',TPropertyValue::ensureString($value),'');
	}

	/**
	 * This method overrides the parent's implementation.
	 * The validation succeeds if the input control has the same value
	 * as the one displayed in the corresponding CAPTCHA control.
	 *
	 * @return boolean whether the validation succeeds
	 */
	protected function evaluateIsValid()
	{
		$value=$this->getValidationValue($this->getValidationTarget());
		$control=$this->findCaptchaControl();
		return $control->validate(trim($value));
	}

	/**
	 * @return TCaptchaControl the CAPTCHA control to be validated against
	 * @throws TConfigurationException if the CAPTCHA cannot be found according to {@link setCaptchaControl CaptchaControl}
	 */
	protected function findCaptchaControl()
	{
		if(($id=$this->getCaptchaControl())==='')
			throw new TConfigurationException('captchavalidator_captchacontrol_required');
		else if(($control=$this->findControl($id))===null)
			throw new TConfigurationException('captchavalidator_captchacontrol_inexistent',$id);
		else if(!($control instanceof TCaptcha))
			throw new TConfigurationException('captchavalidator_captchacontrol_invalid',$id);
		else
			return $control;
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	protected function getClientScriptOptions()
	{
		$options=parent::getClientScriptOptions();
		$control=$this->findCaptchaControl();
		if($control->getCaseSensitive())
		{
			$options['TokenHash']=$this->generateTokenHash($control->getToken());
			$options['CaseSensitive']=true;
		}
		else
		{
			$options['TokenHash']=$this->generateTokenHash(strtoupper($control->getToken()));
			$options['CaseSensitive']=false;
		}
		return $options;
	}

	private function generateTokenHash($token)
	{
		for($h=0,$i=strlen($token)-1;$i>=0;--$i)
			$h+=ord($token[$i]);
		return $h;
	}
}

