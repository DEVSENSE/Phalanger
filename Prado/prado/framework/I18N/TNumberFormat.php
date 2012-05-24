<?php
/**
 * TNumberFromat component.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TNumberFormat.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.I18N
 */

/**
 * Get the NumberFormat class.
 */
Prado::using('System.I18N.core.NumberFormat');

/**
 * Get the parent control class.
 */
Prado::using('System.I18N.TI18NControl');

/**
  * To format numbers in locale sensitive manner use
  * <code>
  * <com:TNumberFormat Pattern="0.##" value="2.0" />
  * </code>
  *
  * Numbers can be formatted as currency, percentage, decimal or scientific
  * numbers by specifying the Type attribute. The known types are
  * "currency", "percentage", "decimal" and "scientific".
  *
  * If someone from US want to see sales figures from a store in
  * Germany (say using the EURO currency), formatted using the german
  * currency, you would need to use the attribute Culture="de_DE" to get
  * the currency right, e.g. 100,00. The decimal and grouping separator is
  * then also from the de_DE locale. This may lead to some confusion because
  * people from US know the "," as thousand separator. Therefore a "Currency"
  * attribute is available, so that the output from the following example
  * results in 100.00.
  * <code>
  * <com:TNumberFormat Type="currency" Culture="en_US" Currency="EUR" Value="100" />
  * </code>
  *
  * Namespace: System.I18N
  *
  * Properties
  * - <b>Value</b>, number,
  *   <br>Gets or sets the number to format. The tag content is used as Value
  *   if the Value property is not specified.
  * - <b>Type</b>, string,
  *   <br>Gets or sets the formatting type. The valid types are
  *    'decimal', 'currency', 'percentage' and 'scientific'.
  * - <b>Currency</b>, string,
  *   <br>Gets or sets the currency symbol for the currency format.
  *   The default is 'USD' if the Currency property is not specified.
  * - <b>Pattern</b>, string,
  *   <br>Gets or sets the custom number formatting pattern.
  * - <b>DefaultText</b>, string,
  * <br>Gets or sets the default text. If Value is not set, DefaultText will be
  * shown instead of the default currency Value/Pattern.
  *
  * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
  * @version v1.0, last update on Sat Dec 11 17:49:56 EST 2004
  * @package System.I18N
  */
class TNumberFormat extends TI18NControl implements IDataRenderer
{
	/**
	 * Default NumberFormat, set to the application culture.
	 * @var NumberFormat
	 */
	protected static $formatter;

	/**
	 * Get the number formatting pattern.
	 * @return string format pattern.
	 */
	public function getPattern()
	{
		return $this->getViewState('Pattern','');
	}

	/**
	 * Set the number format pattern.
	 * @param string format pattern.
	 */
	public function setPattern($pattern)
	{
		$this->setViewState('Pattern',$pattern,'');
	}

	/**
	 * Get the numberic value for this control.
	 * @return string number
	 */
	public function getValue()
	{
		return $this->getViewState('Value','');
	}

	/**
	 * Set the numberic value for this control.
	 * @param string the number value
	 */
	public function setValue($value)
	{
		$this->setViewState('Value',$value,'');
	}

	/**
	 * Get the default text value for this control.
	 * @return string default text value
	 */
	public function getDefaultText()
	{
		return $this->getViewState('DefaultText','');
	}
	
	/**
	 * Set the default text value for this control.
	 * @param string default text value
	 */
	public function setDefaultText($value)
	{
		$this->setViewState('DefaultText',$value,'');
	}

	/**
	 * Get the numberic value for this control.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getValue()}.
	 * @return string number
	 * @see getValue
	 * @since 3.1.2
	 */
	public function getData()
	{
		return $this->getValue();
	}

	/**
	 * Set the numberic value for this control.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setValue()}.
	 * @param string the number value
	 * @see setValue
	 * @since 3.1.2
	 */
	public function setData($value)
	{
		$this->setValue($value);
	}

	/**
	 * Get the formatting type for this control.
	 * @return string formatting type.
	 */
	public function getType()
	{
		return $this->getViewState('Type','d');
	}

	/**
	 * Set the formatting type for this control.
	 * @param string formatting type, either "decimal", "currency","percentage"
	 * or "scientific"
	 * @throws TPropertyTypeInvalidException
	 */
	public function setType($type)
	{
		$type = strtolower($type);

		switch($type)
		{
			case 'decimal':
				$this->setViewState('Type','d',''); break;
			case 'currency':
				$this->setViewState('Type','c',''); break;
			case 'percentage':
				$this->setViewState('Type','p',''); break;
			case 'scientific':
				$this->setViewState('Type','e',''); break;
			default:
				throw new TInvalidDataValueException('numberformat_type_invalid',$type);
		}

	}

	/**
	 * @return string 3 letter currency code. Defaults to 'USD'.
	 */
	public function getCurrency()
	{
		return $this->getViewState('Currency','USD');
	}

	/**
	 * Set the 3-letter ISO 4217 code. For example, the code
	 * "USD" represents the US Dollar and "EUR" represents the Euro currency.
	 * @param string currency code.
	 */
	public function setCurrency($currency)
	{
		$this->setViewState('Currency', $currency,'');
	}

	/**
	 * Formats the localized number, be it currency or decimal, or percentage.
	 * If the culture is not specified, the default application
	 * culture will be used.
	 * @return string formatted number
	 */
	protected function getFormattedValue()
	{
		$value = $this->getValue();
		$defaultText = $this->getDefaultText();
		if(empty($value) && !empty($defaultText))
			return $this->getDefaultText();

		$app = $this->getApplication()->getGlobalization();
		//initialized the default class wide formatter
		if(self::$formatter===null)
			self::$formatter = new NumberFormat($app->getCulture());

		$pattern = strlen($this->getPattern()) > 0
						? $this->getPattern() : $this->getType();

		$culture = $this->getCulture();
		//return the specific cultural formatted number
		if(!empty($culture) && $app->getCulture() != $culture)
		{
			$formatter = new NumberFormat($culture);
			return $formatter->format($this->getValue(),$pattern,
									  $this->getCurrency(),
									  $this->getCharset());
		}

		//return the application wide culture formatted number.
		return self::$formatter->format($this->getValue(),$pattern,
										$this->getCurrency(),
										$this->getCharset());
	}

	public function render($writer)
	{
		$writer->write($this->getFormattedValue());
	}
}

?>
