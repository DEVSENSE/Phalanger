<?php
/**
 * TSimpleDateFormatter class file
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSimpleDateFormatter.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Util
 */

/**
 * TSimpleDateFormatter class.
 *
 * Formats and parses dates using the SimpleDateFormat pattern.
 * This pattern is compatible with the I18N and java's SimpleDateFormatter.
 * <code>
 * Pattern |      Description
 * ----------------------------------------------------
 * d       | Day of month 1 to 31, no padding
 * dd      | Day of monath 01 to 31, zero leading
 * M       | Month digit 1 to 12, no padding
 * MM      | Month digit 01 to 12, zero leading
 * yy      | 2 year digit, e.g., 96, 05
 * yyyy    | 4 year digit, e.g., 2005
 * ----------------------------------------------------
 * </code>
 *
 * Usage example, to format a date
 * <code>
 * $formatter = new TSimpleDateFormatter("dd/MM/yyy");
 * echo $formatter->format(time());
 * </code>
 *
 * To parse the date string into a date timestamp.
 * <code>
 * $formatter = new TSimpleDateFormatter("d-M-yyy");
 * echo $formatter->parse("24-6-2005");
 * </code>
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TSimpleDateFormatter.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.0
 */
class TSimpleDateFormatter
{
	/**
	 * Formatting pattern.
	 * @var string
	 */
	private $pattern;

	/**
	 * Charset, default is 'UTF-8'
	 * @var string
	 */
	private $charset = 'UTF-8';

	/**
	 * Constructor, create a new date time formatter.
	 * @param string formatting pattern.
	 * @param string pattern and value charset
	 */
	public function __construct($pattern, $charset='UTF-8')
	{
		$this->setPattern($pattern);
		$this->setCharset($charset);
	}

	/**
	 * @return string formatting pattern.
	 */
	public function getPattern()
	{
		return $this->pattern;
	}

	/**
	 * @param string formatting pattern.
	 */
	public function setPattern($pattern)
	{
		$this->pattern = $pattern;
	}

	/**
	 * @return string formatting charset.
	 */
	public function getCharset()
	{
		return $this->charset;
	}

	/**
	 * @param string formatting charset.
	 */
	public function setCharset($charset)
	{
		$this->charset = $charset;
	}

	/**
	 * Format the date according to the pattern.
	 * @param string|int the date to format, either integer or a string readable by strtotime.
	 * @return string formatted date.
	 */
	public function format($value)
	{
		$date = $this->getDate($value);
		$bits['yyyy'] = $date['year'];
		$bits['yy'] = substr("{$date['year']}", -2);

		$bits['MM'] = str_pad("{$date['mon']}", 2, '0', STR_PAD_LEFT);
		$bits['M'] = $date['mon'];

		$bits['dd'] = str_pad("{$date['mday']}", 2, '0', STR_PAD_LEFT);
		$bits['d'] = $date['mday'];

		$pattern = preg_replace('/M{3,4}/', 'MM', $this->pattern);
		return str_replace(array_keys($bits), $bits, $pattern);
	}

	public function getMonthPattern()
	{
		if(is_int(strpos($this->pattern, 'MMMM')))
			return 'MMMM';
		if(is_int(strpos($this->pattern, 'MMM')))
			return 'MMM';
		if(is_int(strpos($this->pattern, 'MM')))
			return 'MM';
		if(is_int(strpos($this->pattern, 'M')))
			return 'M';
		return false;
	}

	public function getDayPattern()
	{
		if(is_int(strpos($this->pattern, 'dd')))
			return 'dd';
		if(is_int(strpos($this->pattern, 'd')))
			return 'd';
		return false;
	}

	public function getYearPattern()
	{
		if(is_int(strpos($this->pattern, 'yyyy')))
			return 'yyyy';
		if(is_int(strpos($this->pattern, 'yy')))
			return 'yy';
		return false;
	}

	public function getDayMonthYearOrdering()
	{
		$ordering = array();
		if(is_int($day= strpos($this->pattern, 'd')))
			$ordering['day'] = $day;
		if(is_int($month= strpos($this->pattern, 'M')))
			$ordering['month'] = $month;
		if(is_int($year= strpos($this->pattern, 'yy')))
			$ordering['year'] = $year;
		asort($ordering);
		return array_keys($ordering);
	}

	/**
	 * Gets the time stamp from string or integer.
	 * @param string|int date to parse
	 * @return array date info array
	 */
	private function getDate($value)
	{
		if(!is_string($value))
		{
			$s = Prado::createComponent('System.Util.TDateTimeStamp');
			return $s->getDate($value);
		}
		$date = @strtotime($value);
		if($date < 0)
			throw new TInvalidDataValueException('invalid_date', $value);
		return @getdate($date);
	}

	/**
	 * @return boolean true if the given value matches with the date pattern.
	 */
	public function isValidDate($value)
	{
		if($value === null) {
			return false;
		} else {
			return $this->parse($value, false) !== null;
		}
	}

	/**
	 * Parse the string according to the pattern.
	 * @param string|int date string or integer to parse
	 * @return int date time stamp
	 * @throws TInvalidDataValueException if date string is malformed.
	 */
	public function parse($value,$defaultToCurrentTime=true)
	{
		if(is_int($value) || is_float($value))
			return $value;
		else if(!is_string($value))
			throw new TInvalidDataValueException('date_to_parse_must_be_string', $value);

		if(empty($this->pattern)) return time();

		$date = time();

		if($this->length(trim($value)) < 1)
			return $defaultToCurrentTime ? $date : null;

		$pattern = $this->pattern;

		$i_val = 0;
		$i_format = 0;
		$pattern_length = $this->length($pattern);
		$c = '';
		$token='';
		$x=null; $y=null;


		if($defaultToCurrentTime)
		{
			$year = "{$date['year']}";
			$month = $date['mon'];
			$day = $date['mday'];
		}
		else
		{
			$year = null;
			$month = null;
			$day = null;
		}

		while ($i_format < $pattern_length)
		{
			$c = $this->charAt($pattern,$i_format);
			$token='';
			while ($this->charEqual($pattern, $i_format, $c)
						&& ($i_format < $pattern_length))
			{
				$token .= $this->charAt($pattern, $i_format++);
			}

			if ($token=='yyyy' || $token=='yy' || $token=='y')
			{
				if ($token=='yyyy') { $x=4;$y=4; }
				if ($token=='yy')   { $x=2;$y=2; }
				if ($token=='y')    { $x=2;$y=4; }
				$year = $this->getInteger($value,$i_val,$x,$y);
				if($year === null)
					return null;
					//throw new TInvalidDataValueException('Invalid year', $value);
				$i_val += strlen($year);
				if(strlen($year) == 2)
				{
					$iYear = (int)$year;
					if($iYear > 70)
						$year = $iYear + 1900;
					else
						$year = $iYear + 2000;
				}
				$year = (int)$year;
			}
			elseif($token=='MM' || $token=='M')
			{
				$month=$this->getInteger($value,$i_val,
									$this->length($token),2);
				$iMonth = (int)$month;
				if($month === null || $iMonth < 1 || $iMonth > 12 )
					return null;
					//throw new TInvalidDataValueException('Invalid month', $value);
				$i_val += strlen($month);
				$month = $iMonth;
			}
			elseif ($token=='dd' || $token=='d')
			{
				$day = $this->getInteger($value,$i_val,
									$this->length($token), 2);
				$iDay = (int)$day;
				if($day === null || $iDay < 1 || $iDay >31)
					return null;
					//throw new TInvalidDataValueException('Invalid day', $value);
				$i_val += strlen($day);
				$day = $iDay;
			}
			else
			{
				if($this->substring($value, $i_val, $this->length($token)) != $token)
					return null;
					//throw new TInvalidDataValueException("Subpattern '{$this->pattern}' mismatch", $value);
				else
					$i_val += $this->length($token);
			}
		}
		if ($i_val != $this->length($value))
			return null;
			//throw new TInvalidDataValueException("Pattern '{$this->pattern}' mismatch", $value);
		if(!$defaultToCurrentTime && ($month === null || $day === null || $year === null))
			return null;
		else
		{		
			if(empty($year)) {
				$year = date('Y');
			}
			$day = (int)$day <= 0 ? 1 : (int)$day;
			$month = (int)$month <= 0 ? 1 : (int)$month;
			$s = Prado::createComponent('System.Util.TDateTimeStamp');
			return $s->getTimeStamp(0, 0, 0, $month, $day, $year);
		}
	}

	/**
	 * Calculate the length of a string, may be consider iconv_strlen?
	 */
	private function length($string)
	{
		//use iconv_strlen or just strlen?
		return strlen($string);
	}

	/**
	 * Get the char at a position.
	 */
	private function charAt($string, $pos)
	{
		return $this->substring($string, $pos, 1);
	}

	/**
	 * Gets a portion of a string, uses iconv_substr.
	 */
	private function substring($string, $start, $length)
	{
		return iconv_substr($string, $start, $length);
	}

	/**
	 * Returns true if char at position equals a particular char.
	 */
	private function charEqual($string, $pos, $char)
	{
		return $this->charAt($string, $pos) == $char;
	}

	/**
	 * Gets integer from part of a string, allows integers of any length.
	 * @param string string to retrieve the integer from.
	 * @param int starting position
	 * @param int minimum integer length
	 * @param int maximum integer length
	 * @return string integer portion of the string, null otherwise
	 */
	private function getInteger($str,$i,$minlength,$maxlength)
	{
		//match for digits backwards
		for ($x = $maxlength; $x >= $minlength; $x--)
		{
			$token= $this->substring($str, $i,$x);
			if ($this->length($token) < $minlength)
				return null;
			if (preg_match('/^\d+$/', $token))
				return $token;
		}
		return null;
	}
}

?>
