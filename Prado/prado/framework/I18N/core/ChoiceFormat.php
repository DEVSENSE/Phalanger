<?php

/**
 * ChoiceFormat class file.
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
 * @version $Revision: 1.1 $  $Date: 2005/01/11 07:19:39 $
 * @package System.I18N.core
 */


/**
 * ChoiceFormat class.
 *
 * ChoiceFormat converts between ranges of numeric values and string
 * names for those ranges.
 *
 * A ChoiceFormat splits the real number line -Inf to +Inf into two or
 * more contiguous ranges. Each range is mapped to a string.
 * ChoiceFormat is generally used in a MessageFormat for displaying
 * grammatically correct plurals such as "There are 2 files."
 *
 * <code>
 *	$string = '[0] are no files |[1] is one file |(1,Inf] are {number} files';
 *
 *  $formatter = new MessageFormat(...); //init for a source
 *	$translated = $formatter->format($string);
 *
 *  $choice = new ChoiceFormat();
 * 	echo $choice->format($translated, 0); //shows "are no files"
 * </code>
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
 * @version v1.0, last update on Fri Dec 24 20:46:16 EST 2004
 * @package System.I18N.core
 */
class ChoiceFormat
{
	/**
	 * The pattern to validate a set notation
	 * @var string
	 */
	protected $validate = '/[\(\[\{]|[-Inf\d:\s]+|,|[\+Inf\d\s:\?\-=!><%\|&\(\)]+|[\)\]\}]/ms';

	/**
	 * The pattern to parse the formatting string.
	 * @var string
	 */
	protected $parse = '/\s*\|?([\(\[\{]([-Inf\d:\s]+,?[\+Inf\d\s:\?\-=!><%\|&\(\)]*)+[\)\]\}])\s*/';

	/**
	 * The value for positive infinity.
	 * @var float
	 */
	protected $inf;


	/**
	 * Constructor.
	 */
	function __construct()
	{
		$this->inf = -log(0);
	}


	/**
	 * Determine if the given number belongs to a given set
	 * @param float the number to test.
	 * @param string the set, in set notation.
	 * @return boolean true if number is in the set, false otherwise.
	 */
	function isValid($number, $set)
	{
		$n = preg_match_all($this->validate,$set,$matches,PREG_SET_ORDER);

		if($n < 3) throw new Exception("Invalid set \"{$set}\"");

		if(preg_match('/\{\s*n:([^\}]+)\}/', $set, $def))
		{
			return $this->isValidSetNotation($number, $def[1]);
		}

		$leftBracket = $matches[0][0];
		$rightBracket = $matches[$n-1][0];

		$i = 0;
		$elements = array();
		foreach($matches as $match)
		{
			$string = $match[0];
			if($i != 0 && $i != $n-1 && $string !== ',')
			{
				if($string == '-Inf')
					$elements[] = -1*$this->inf;
				else if ($string == '+Inf' || $string == 'Inf')
					$elements[] = $this->inf;
				else
					$elements[] = floatval($string);
			}
			$i++;
		}
		$total = count($elements);
		$number = floatval($number);

		if($leftBracket == '{' && $rightBracket == '}')
			return in_array($number, $elements);

		$left = false;
		if($leftBracket == '[')
			$left = $number >= $elements[0];
		else if ($leftBracket == '(')
			$left = $number > $elements[0];

		$right = false;
		if($rightBracket==']')
			$right = $number <= $elements[$total-1];
		else if($rightBracket == ')')
			$right = $number < $elements[$total-1];

		if($left && $right) return true;

		return false;
	}

	protected function isValidSetNotation($number, $set)
	{
		$str = '$result = '.str_replace('n', '$number', $set).';';
		try
		{
			eval($str);
			return $result;
		}
		catch(Exception $e)
		{
			return false;
		}
	}

	/**
	 * Parse a choice string and get a list of sets and a list of strings
	 * corresponding to the sets.
	 * @param string the string containing the choices
	 * @return array array($sets, $strings)
	 */
	function parse($string)
	{
		$n = preg_match_all($this->parse,$string,$matches, PREG_OFFSET_CAPTURE);
		$sets = array();
		foreach($matches[1] as $match)
			$sets[] = $match[0];
		$offset = $matches[0];
		$strings = array();
		for($i = 0; $i < $n; $i++)
		{
			$len = strlen($offset[$i][0]);
			$begin = $i == 0? $len : $offset[$i][1] + $len;
			$end = $i == $n-1 ? strlen($string) : $offset[$i+1][1];
			$strings[] = substr($string, $begin, $end - $begin);
		}
		return array($sets, $strings);
	}

	/**
	 * For the choice string, and a number, find and return the
	 * string that satisfied the set within the choices.
	 * @param string the choices string.
	 * @param float the number to test.
	 * @return string the choosen string.
	 */
	public function format($string, $number)
	{
		list($sets, $strings) = $this->parse($string);
		$total = count($sets);
		for($i = 0; $i < $total; $i++)
		{
			if($this->isValid($number, $sets[$i]))
				return $strings[$i];
		}
		return false;
	}
}

?>
