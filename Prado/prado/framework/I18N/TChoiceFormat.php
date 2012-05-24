<?php
/**
 * TChoiceFormat, I18N choice format component.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TChoiceFormat.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.I18N
 */

 /**
 * Get the ChoiceFormat class.
 */
Prado::using('System.I18N.core.ChoiceFormat');
Prado::using('System.I18N.TTranslate');

/**
 * TChoiceFormat class.
 *
 * This component performs message/string choice translation. The translation
 * source is set in the TGlobalization module. The following example
 * demonstrates a simple 2 choice message translation.
 * <code>
 * <com:TChoiceFormat Value="1">[1] One Apple. |[2] Two Apples</com:TChoiceFormat>
 * </code>
 *
 * The Choice has <b>Value</b> "1" (one), thus the translated string
 * is "One Apple". If the <b>Value</b> is "2", then it will show
 * "Two Apples".
 *
 * The message/string choices are separated by the pipe "|" followed
 * by a set notation of the form
 *  # <tt>[1,2]</tt> -- accepts values between 1 and 2, inclusive.
 *  # <tt>(1,2)</tt> -- accepts values between 1 and 2, excluding 1 and 2.
 *  # <tt>{1,2,3,4}</tt> -- only values defined in the set are accepted.
 *  # <tt>[-Inf,0)</tt> -- accepts value greater or equal to negative infinity
 *                       and strictly less than 0
 * Any non-empty combinations of the delimiters of square and round brackets
 * are acceptable.
 *
 * The string choosen for display depends on the <b>Value</b> property.
 * The <b>Value</b> is evaluated for each set until the Value is found
 * to belong to a particular set.
 *
 * Properties
 * - <b>Value</b>, float,
 *   <br>Gets or sets the Value that determines which string choice to display.
 * Since version 3.1.2 the following set notation is also possible.
 *
 *  # <tt>{n: n % 10 > 1 && n % 10 < 5}</tt> --  matches numbers like 2, 3, 4, 22, 23, 24
 *
 * Where set is defined by the expression after <tt>n:</tt>. In particular, the expression
 * accepts the following mathematical/logical operators to form a set of logical conditions
 * on the value given by <tt>n</tt>:
 *   # <tt>&lt;</tt> -- less than.
 *   # <tt>&lt;=</tt> -- less than equals.
 *   # <tt>&gt;</tt> -- greater than.
 *   # <tt>&gt=</tt> -- greater than equals.
 *   # <tt>==</tt> -- of equal value.
 *   # <tt>%</tt> -- modulo, e.g., 1 % 10 equals 1, 11 % 10 equals 1.
 *   # <tt>-</tt> -- minus, negative.
 *   # <tt>+</tt> -- addition.
 *   # <tt>&amp;</tt> -- conditional AND.
 *   # <tt>&amp;&amp;</tt> -- condition AND with short circuit.
 *   # <tt>|</tt> -- conditional OR.
 *   # <tt>||</tt> -- conditional OR with short circuit. 
 *   # <tt>!</tt> -- negation.
 *
 * Additional round brackets can also be used to perform grouping.
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version v1.0, last update on Fri Dec 24 21:38:49 EST 2004
 * @package System.I18N
 */
class TChoiceFormat extends TTranslate
{
	/**
	 * @return float the numerical value.
	 */
	public function getValue()
	{
		return $this->getViewState('Value','');
	}

	/**
	 * Sets the numerical choice value
	 * @param float the choice value
	 */
	public function setValue($value)
	{
		$this->setViewState('Value',$value,'');
	}

	/**
	 * Display the choosen translated string.
	 * Overrides the parent method, also calls parent's renderBody to
	 * translate.
	 */
	protected function translateText($text, $subs)
	{
		$text = parent::translateText($text, $subs);
		$choice = new ChoiceFormat();
		$value = $this->getValue();
		$string = $choice->format($text, $value);
		if($string)
			return strtr($string, array('{Value}'=> $value));
	}
}
?>
