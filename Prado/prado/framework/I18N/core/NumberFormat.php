<?php

/**
 * NumberFormat class file.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the BSD License.
 *
 * Copyright(c) 2004 by Qiang Xue. All rights reserved.
 *
 * To contact the author write to {@link mailto:qiang.xue@gmail.com Qiang Xue}
 * The latest version of PRADO can be obtained from:
 * {@link http://prado.sourceforge.net/}
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Revision: 1.6 $  $Date: 2005/12/20 09:32:42 $
 * @package System.I18N.core
 */

/**
 * Get the NumberFormatInfo class file.
 */
require_once(dirname(__FILE__).'/NumberFormatInfo.php');


/**
 * Get the encoding utilities
 */
require_once(dirname(__FILE__).'/util.php');


/**
 * NumberFormat class.
 *
 * NumberFormat formats decimal numbers in any locale. The decimal
 * number is formatted according to a particular pattern. These
 * patterns can arise from the NumberFormatInfo object which is
 * culturally sensitive. The NumberFormat class can be instantiated in
 * many ways. E.g.
 *
 * <code>
 *  //create a invariant number formatter.
 *	$formatter = new NumberFormat();
 *
 *  //create a number format for the french language locale.
 *  $fr = new NumberFormat('fr');
 *
 *  //create a number format base on a NumberFormatInfo instance $numberInfo.
 *  $format = new NumberFormat($numberInfo);
 * </code>
 *
 * A normal decimal number can also be displayed as a currency
 * or as a percentage. For example
 * <code>
 * $format->format(1234.5); //Decimal number "1234.5"
 * $format->format(1234.5,'c'); //Default currency "$1234.50"
 * $format->format(0.25, 'p') //Percent "25%"
 * </code>
 *
 * Currency is formated using the localized currency pattern. For example
 * to format the number as Japanese Yen:
 * <code>
 *  $ja = new NumberFormat('ja_JP');
 *
 *  //Japanese currency pattern, and using Japanese Yen symbol
 *  $ja->format(123.14,'c','JPY'); //�?123 (Yen 123)
 * </code>
 * For each culture, the symbol for each currency may be different.
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version v1.0, last update on Fri Dec 10 18:10:20 EST 2004
 * @package System.I18N.core
 */
class NumberFormat
{

	/**
	 * The DateTimeFormatInfo, containing culture specific patterns and names.
 	 * @var DateTimeFormatInfo
	 */
	protected $formatInfo;

	/**
	 * Create a new number format instance. The constructor can be instantiated
	 * with a string that represent a culture/locale. Similarly, passing
	 * a CultureInfo or NumberFormatInfo instance will instantiated a instance
	 * for that particular culture.
	 * @param mixed either null, a CultureInfo, a NumberFormatInfo, or string
	 * @return NumberFormat
	 */
	function __construct($formatInfo=null)
	{
		if($formatInfo === null)
			$this->formatInfo = NumberFormatInfo::getInvariantInfo();
		else if($formatInfo instanceof CultureInfo)
			$this->formatInfo = $formatInfo->NumberFormat;
		else if($formatInfo instanceof NumberFormatInfo)
			$this->formatInfo = $formatInfo;
		else
			$this->formatInfo =
				NumberFormatInfo::getInstance($formatInfo);
	}

	/**
	 * For the number for a certain pattern. The valid patterns are
	 * 'c', 'd', 'e', 'p' or a custom pattern, such as "#.000" for
	 * 3 decimal places.
	 * @param mixed the number to format.
	 * @param string the format pattern, either, 'c', 'd', 'e', 'p'
	 * or a custom pattern. E.g. "#.000" will format the number to
	 * 3 decimal places.
	 * @param string 3-letter ISO 4217 code. For example, the code
	 * "USD" represents the US Dollar and "EUR" represents the Euro currency.
	 * @return string formatted number string
	 */
	function format($number, $pattern='d', $currency='USD', $charset='UTF-8')
	{
		$this->setPattern($pattern);

		if(strtolower($pattern) == 'p')
			$number = $number * 100;

		$string = (string)$number;

		$decimal = $this->formatDecimal($string);
		$integer = $this->formatInteger(abs($number));

		if(strlen($decimal)>0)
			$result = $integer.$decimal;
		else
			$result = $integer;

		//get the suffix
		if($number >= 0)
			$suffix = $this->formatInfo->PositivePattern;
		else if($number < 0)
			$suffix = $this->formatInfo->NegativePattern;
		else
			$suffix = array("","");

		//append and prepend suffix
		$result = $suffix[0].$result.$suffix[1];

		//replace currency sign
		$symbol = @$this->formatInfo->getCurrencySymbol($currency);
		if($symbol === null) {
			$symbol = $currency;
		}

		$result = str_replace('¤',$symbol, $result);

		return I18N_toEncoding($result, $charset);
	}

	/**
	 * For the integer, perform groupings and string padding.
	 * @param string the decimal number in string form.
	 * @return string  formatted integer string with grouping
	 */
	protected function formatInteger($string)
	{
		$string = (string)$string;

		$decimalDigits = $this->formatInfo->DecimalDigits;
		//if not decimal digits, assume 0 decimal points.
		if(is_int($decimalDigits) && $decimalDigits > 0)
			$string = (string)round(floatval($string),$decimalDigits);
		$dp = strpos($string, '.');
		if(is_int($dp))
			$string = substr($string, 0, $dp);
		$integer = '';

		$digitSize = $this->formatInfo->getDigitSize();

		$string = str_pad($string, $digitSize, '0',STR_PAD_LEFT);

		$len = strlen($string);

		$groupSeparator = $this->formatInfo->GroupSeparator;
		$groupSize = $this->formatInfo->GroupSizes;


		$firstGroup = true;
		$multiGroup = is_int($groupSize[1]);
		$count = 0;

		if(is_int($groupSize[0]))
		{
			//now for the integer groupings
			for($i=0; $i<$len; $i++)
			{
				$char = $string{$len-$i-1};

				if($multiGroup && $count == 0)
				{
					if($i != 0 && $i%$groupSize[0] == 0)
					{
						$integer = $groupSeparator . $integer;
						$count++;
					}
				}
				else if($multiGroup && $count >= 1)
				{
					if($i != 0 && ($i-$groupSize[0])%$groupSize[1] == 0)
					{
						$integer = $groupSeparator . $integer;
						$count++;
					}
				}
				else
				{
					if($i != 0 && $i%$groupSize[0] == 0)
					{
						$integer = $groupSeparator . $integer;
						$count++;
					}
				}

				$integer = $char . $integer;
			}
		}
		else
			$integer = $string;

		return $integer;
	}

	/**
	 * Format the decimal places.
	 * @param string the decimal number in string form.
	 * @return string formatted decimal places.
	 */
	protected function formatDecimal($string)
	{
		$dp = strpos($string, '.');
		$decimal = '';

		$decimalDigits = $this->formatInfo->DecimalDigits;
		$decimalSeparator = $this->formatInfo->DecimalSeparator;

		//do the correct rounding here
		//$string = round(floatval($string), $decimalDigits);
		if(is_int($dp))
		{
			if($decimalDigits == -1)
			{
				$decimal = substr($string, $dp+1);
			}
			else if(is_int($decimalDigits))
			{
				$float = round((float)$string, $decimalDigits);
				if(strpos((string)$float, '.') === false)
				{
					$decimal = str_pad($decimal,$decimalDigits,'0');
				}
				else
				{
					$decimal = substr($float, strpos($float,'.')+1);
					if(strlen($decimal)<$decimalDigits)
						$decimal = str_pad($decimal,$decimalDigits,'0');
				}
			}
			else
				return $decimal;

			return $decimalSeparator.$decimal;
		}
		else if ($decimalDigits > 0)
			return $decimalSeparator.str_pad($decimal,$decimalDigits,'0');

		return $decimal;
	}

	/**
	 * Set the pattern to format against. The default patterns
	 * are retrieved from the NumberFormatInfo instance.
	 * @param string the requested patterns.
	 * @return string a number format pattern.
	 */
	protected function setPattern($pattern)
	{
		switch($pattern)
		{
			case 'c':
			case 'C':
				$this->formatInfo->setPattern(NumberFormatInfo::CURRENCY);
				break;
			case 'd':
			case 'D':
				$this->formatInfo->setPattern(NumberFormatInfo::DECIMAL);
				break;
			case 'e':
			case 'E':
				$this->formatInfo->setPattern(NumberFormatInfo::SCIENTIFIC);
				break;
			case 'p':
			case 'P':
				$this->formatInfo->setPattern(NumberFormatInfo::PERCENTAGE);
				break;
			default:
				$this->formatInfo->setPattern($pattern);
				break;
		}
	}
}

