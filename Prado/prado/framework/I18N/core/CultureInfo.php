<?php

/**
 * CultureInfo class file.
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
 * @version $Id: CultureInfo.php 2668 2009-05-29 06:54:24Z Christophe.Boulain $
 * @package System.I18N.core
 */

/**
 * CultureInfo class.
 *
 * Represents information about a specific culture including the
 * names of the culture, the calendar used, as well as access to
 * culture-specific objects that provide methods for common operations,
 * such as formatting dates, numbers, and currency.
 *
 * The CultureInfo class holds culture-specific information, such as the
 * associated language, sublanguage, country/region, calendar, and cultural
 * conventions. This class also provides access to culture-specific
 * instances of DateTimeFormatInfo and NumberFormatInfo. These objects
 * contain the information required for culture-specific operations,
 * such as formatting dates, numbers and currency.
 *
 * The culture names follow the format "<languagecode>_<country/regioncode>",
 * where <languagecode> is a lowercase two-letter code derived from ISO 639
 * codes. You can find a full list of the ISO-639 codes at
 * http://www.ics.uci.edu/pub/ietf/http/related/iso639.txt
 *
 * The <country/regioncode2> is an uppercase two-letter code derived from
 * ISO 3166. A copy of ISO-3166 can be found at
 * http://www.chemie.fu-berlin.de/diverse/doc/ISO_3166.html
 *
 * For example, Australian English is "en_AU".
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: CultureInfo.php 2668 2009-05-29 06:54:24Z Christophe.Boulain $
 * @package System.I18N.core
 */
class CultureInfo
{
	/**
	 * ICU data filename extension.
	 * @var string
	 */
	private $dataFileExt = '.dat';

	/**
	 * The ICU data array.
	 * @var array
	 */
	private $data = array();

	/**
	 * The current culture.
	 * @var string
	 */
	private $culture;

	/**
	 * Directory where the ICU data is stored.
	 * @var string
	 */
	private $dataDir;

	/**
	 * A list of ICU date files loaded.
	 * @var array
	 */
	private $dataFiles = array();

	/**
	 * The current date time format info.
	 * @var DateTimeFormatInfo
	 */
	private $dateTimeFormat;

	/**
	 * The current number format info.
	 * @var NumberFormatInfo
	 */
	private $numberFormat;

	/**
	 * A list of properties that are accessable/writable.
	 * @var array
	 */
	protected $properties = array();

	/**
	 * Culture type, all.
	 * @see getCultures()
	 * @var int
	 */
	const ALL = 0;

	/**
	 * Culture type, neutral.
	 * @see getCultures()
	 * @var int
	 */
	const NEUTRAL = 1;

	/**
	 * Culture type, specific.
	 * @see getCultures()
	 * @var int
	 */
	const SPECIFIC = 2;

	/**
	 * Display the culture name.
	 * @return string the culture name.
	 * @see getName()
	 */
	function __toString()
	{
		return $this->getName();
	}


	/**
	 * Allow functions that begins with 'set' to be called directly
	 * as an attribute/property to retrieve the value.
	 * @return mixed
	 */
	function __get($name)
	{
		$getProperty = 'get'.$name;
		if(in_array($getProperty, $this->properties))
			return $this->$getProperty();
		else
			throw new Exception('Property '.$name.' does not exists.');
	}

	/**
	 * Allow functions that begins with 'set' to be called directly
	 * as an attribute/property to set the value.
	 */
	function __set($name, $value)
	{
		$setProperty = 'set'.$name;
		if(in_array($setProperty, $this->properties))
			$this->$setProperty($value);
		else
			throw new Exception('Property '.$name.' can not be set.');
	}


	/**
	 * Initializes a new instance of the CultureInfo class based on the
	 * culture specified by name. E.g. <code>new CultureInfo('en_AU');</cdoe>
	 * The culture indentifier must be of the form
	 * "language_(country/region/variant)".
	 * @param string a culture name, e.g. "en_AU".
	 * @return return new CultureInfo.
	 */
	function __construct($culture='en')
	{
		$this->properties = get_class_methods($this);

		if(empty($culture))
			$culture = 'en';

		$this->dataDir = $this->dataDir();
		$this->dataFileExt = $this->fileExt();

		$this->setCulture($culture);

		$this->loadCultureData('root');
		$this->loadCultureData($culture);
	}

	/**
	 * Get the default directory for the ICU data.
	 * The default is the "data" directory for this class.
	 * @return string directory containing the ICU data.
	 */
	protected static function dataDir()
	{
		return dirname(__FILE__).'/data/';
	}

	/**
	 * Get the filename extension for ICU data. Default is ".dat".
	 * @return string filename extension for ICU data.
	 */
	protected static function fileExt()
	{
		return '.dat';
	}

	/**
	* Gets the CultureInfo that for this culture string
	* @return CultureInfo invariant culture info is "en".
	*/
	public static function getInstance($culture)
	{
		static $instances = array();
		if(!isset($instances[$culture]))
			$instances[$culture] = new CultureInfo($culture);
		return $instances[$culture];
	}

	/**
	 * Determine if a given culture is valid. Simply checks that the
	 * culture data exists.
	 * @param string a culture
	 * @return boolean true if valid, false otherwise.
	 */
	public static function validCulture($culture)
	{
		if(preg_match('/^[_\\w]+$/', $culture))
			return is_file(self::dataDir().$culture.self::fileExt());

		return false;
	}

	/**
	 * Set the culture for the current instance. The culture indentifier
	 * must be of the form "<language>_(country/region)".
	 * @param string culture identifier, e.g. "fr_FR_EURO".
	 */
	protected function setCulture($culture)
	{
		if(!empty($culture))
		{
			if (!preg_match('/^[_\\w]+$/', $culture))
				throw new Exception('Invalid culture supplied: ' . $culture);
		}

		$this->culture = $culture;
	}

	/**
	 * Load the ICU culture data for the specific culture identifier.
	 * @param string the culture identifier.
	 */
	protected function loadCultureData($culture)
	{
		$file_parts = explode('_',$culture);
		$current_part = $file_parts[0];

		$files = array($current_part);

		for($i = 1, $k = count($file_parts); $i < $k; ++$i)
		{
			$current_part .= '_'.$file_parts[$i];
			$files[] = $current_part;
		}

		foreach($files as $file)
		{
			$filename = $this->dataDir.$file.$this->dataFileExt;

			if(is_file($filename) == false)
				throw new Exception('Data file for "'.$file.'" was not found.');

			if(in_array($filename, $this->dataFiles) === false)
			{
				array_unshift($this->dataFiles, $file);

				$data = &$this->getData($filename);
				$this->data[$file] = &$data;

				if(isset($data['__ALIAS']))
					$this->loadCultureData($data['__ALIAS'][0]);
				unset($data);
			}
		}
	}

	/**
	 * Get the data by unserializing the ICU data from disk.
	 * The data files are cached in a static variable inside
	 * this function.
	 * @param string the ICU data filename
	 * @return array ICU data
	 */
	protected function &getData($filename)
	{
		static $data = array();
		static $files = array();

		if(!in_array($filename, $files))
		{
			$data[$filename] = unserialize(file_get_contents($filename));
			$files[] = $filename;
		}

		return $data[$filename];
	}

	/**
	 * Find the specific ICU data information from the data.
	 * The path to the specific ICU data is separated with a slash "/".
	 * E.g. To find the default calendar used by the culture, the path
	 * "calendar/default" will return the corresponding default calendar.
	 * Use merge=true to return the ICU including the parent culture.
	 * E.g. The currency data for a variant, say "en_AU" contains one
	 * entry, the currency for AUD, the other currency data are stored
	 * in the "en" data file. Thus to retrieve all the data regarding
	 * currency for "en_AU", you need to use findInfo("Currencies,true);.
	 * @param string the data you want to find.
	 * @param boolean merge the data from its parents.
	 * @return mixed the specific ICU data.
	 */
	protected function findInfo($path='/', $merge=false)
	{
		$result = array();
		foreach($this->dataFiles as $section)
		{
			$info = $this->searchArray($this->data[$section], $path);

			if($info)
			{
				if($merge)
					$result = array_merge($info,$result);
				else
					return $info;
			}
		}

		return $result;
	}

	/**
	 * Search the array for a specific value using a path separated using
	 * slash "/" separated path. e.g to find $info['hello']['world'],
	 * the path "hello/world" will return the corresponding value.
	 * @param array the array for search
	 * @param string slash "/" separated array path.
	 * @return mixed the value array using the path
	 */
	private function searchArray($info, $path='/')
	{
		$index = explode('/',$path);

		$array = $info;

		for($i = 0, $k = count($index); $i < $k; ++$i)
		{
			$value = $index[$i];
			if($i < $k-1 && isset($array[$value]))
				$array = $array[$value];
			else if ($i == $k-1 && isset($array[$value]))
				return $array[$value];
		}
	}

	/**
	 * Gets the culture name in the format
	 * "<languagecode2>_(country/regioncode2)".
	 * @return string culture name.
	 */
	function getName()
	{
		return $this->culture;
	}

	/**
	 * Gets the DateTimeFormatInfo that defines the culturally appropriate
	 * format of displaying dates and times.
	 * @return DateTimeFormatInfo date time format information for the culture.
	 */
	function getDateTimeFormat()
	{
		if($this->dateTimeFormat === null)
		{
			$calendar = $this->getCalendar();
			$info = $this->findInfo("calendar/{$calendar}", true);
			$this->setDateTimeFormat(new DateTimeFormatInfo($info));
		}

		return $this->dateTimeFormat;
	}

	/**
	 * Set the date time format information.
	 * @param DateTimeFormatInfo the new date time format info.
	 */
	function setDateTimeFormat($dateTimeFormat)
	{
		$this->dateTimeFormat = $dateTimeFormat;
	}

	/**
	 * Gets the default calendar used by the culture, e.g. "gregorian".
	 * @return string the default calendar.
	 */
	function getCalendar()
	{
		$info = $this->findInfo('calendar/default');
		return $info[0];
	}

	/**
	 * Gets the culture name in the language that the culture is set
	 * to display. Returns <code>array('Language','Country');</code>
	 * 'Country' is omitted if the culture is neutral.
	 * @return array array with language and country as elements, localized.
	 */
	function getNativeName()
	{
		$lang = substr($this->culture,0,2);
		$reg = substr($this->culture,3,2);
		$language = $this->findInfo("Languages/{$lang}");
		$region = $this->findInfo("Countries/{$reg}");
		if($region)
			return $language[0].' ('.$region[0].')';
		else
			return $language[0];
	}

	/**
	 * Gets the culture name in English.
	 * Returns <code>array('Language','Country');</code>
	 * 'Country' is omitted if the culture is neutral.
	 * @return string language (country), it may locale code string if english name does not exist.
	 */
	function getEnglishName()
	{
		$lang = substr($this->culture,0,2);
		$reg = substr($this->culture,3,2);
		$culture = $this->getInvariantCulture();

		$language = $culture->findInfo("Languages/{$lang}");
		if(count($language) == 0)
			return $this->culture;

		$region = $culture->findInfo("Countries/{$reg}");
		if($region)
			return $language[0].' ('.$region[0].')';
		else
			return $language[0];
	}

	/**
	 * Gets the CultureInfo that is culture-independent (invariant).
	 * Any changes to the invariant culture affects all other
	 * instances of the invariant culture.
	 * The invariant culture is assumed to be "en";
	 * @return CultureInfo invariant culture info is "en".
	 */
	static function getInvariantCulture()
	{
		static $invariant;
		if($invariant === null)
			$invariant = new CultureInfo();
		return $invariant;
	}

	/**
	 * Gets a value indicating whether the current CultureInfo
	 * represents a neutral culture. Returns true if the culture
	 * only contains two characters.
	 * @return boolean true if culture is neutral, false otherwise.
	 */
	function getIsNeutralCulture()
	{
		return strlen($this->culture) == 2;
	}

	/**
	 * Gets the NumberFormatInfo that defines the culturally appropriate
	 * format of displaying numbers, currency, and percentage.
	 * @return NumberFormatInfo the number format info for current culture.
	 */
	function getNumberFormat()
	{
		if($this->numberFormat === null)
		{
			$elements = $this->findInfo('NumberElements');
			$patterns = $this->findInfo('NumberPatterns');
			$currencies = $this->getCurrencies();
			$data = array(	'NumberElements'=>$elements,
							'NumberPatterns'=>$patterns,
							'Currencies' => $currencies);

			$this->setNumberFormat(new NumberFormatInfo($data));
		}
		return $this->numberFormat;
	}

	/**
	 * Set the number format information.
	 * @param NumberFormatInfo the new number format info.
	 */
	function setNumberFormat($numberFormat)
	{
		$this->numberFormat = $numberFormat;
	}

	/**
	 * Gets the CultureInfo that represents the parent culture of the
	 * current CultureInfo
	 * @return CultureInfo parent culture information.
	 */
	function getParent()
	{
		if(strlen($this->culture) == 2)
			return $this->getInvariantCulture();

		$lang = substr($this->culture,0,2);
			return new CultureInfo($lang);
	}

	/**
	 * Gets the list of supported cultures filtered by the specified
	 * culture type. This is an EXPENSIVE function, it needs to traverse
	 * a list of ICU files in the data directory.
	 * This function can be called statically.
	 * @param int culture type, CultureInfo::ALL, CultureInfo::NEUTRAL
	 * or CultureInfo::SPECIFIC.
	 * @return array list of culture information available.
	 */
	static function getCultures($type=CultureInfo::ALL)
	{
		$dataDir = CultureInfo::dataDir();
		$dataExt = CultureInfo::fileExt();
		$dir = dir($dataDir);

		$neutral = array();
		$specific = array();

		while (false !== ($entry = $dir->read()))
		{
			if(is_file($dataDir.$entry)
				&& substr($entry,-4) == $dataExt
				&& $entry != 'root'.$dataExt)
			{
				$culture = substr($entry,0,-4);
				if(strlen($culture) == 2)
					$neutral[] = $culture;
				else
					$specific[] = $culture;
			}
		}
		$dir->close();

		switch($type)
		{
			case CultureInfo::ALL :
				$all = 	array_merge($neutral, $specific);
				sort($all);
				return $all;
				break;
			case CultureInfo::NEUTRAL :
				return $neutral;
				break;
			case CultureInfo::SPECIFIC :
				return $specific;
				break;
		}
	}

	/**
	 * Simplify a single element array into its own value.
	 * E.g. <code>array(0 => array('hello'), 1 => 'world');</code>
	 * becomes <code>array(0 => 'hello', 1 => 'world');</code>
	 * @param array with single elements arrays
	 * @return array simplified array.
	 */
	private function simplify($array)
	{
		for($i = 0, $k = count($array); $i<$k; ++$i)
		{
			$key = key($array);
			if(is_array($array[$key])
				&& count($array[$key]) == 1)
				$array[$key] = $array[$key][0];
			next($array);
		}
		return $array;
	}

	/**
	 * Get a list of countries in the language of the localized version.
	 * @return array a list of localized country names.
	 */
	function getCountries()
	{
		return $this->simplify($this->findInfo('Countries',true));
	}

	/**
	 * Get a list of currencies in the language of the localized version.
	 * @return array a list of localized currencies.
	 */
	function getCurrencies()
	{
		return $this->findInfo('Currencies',true);
	}

	/**
	 * Get a list of languages in the language of the localized version.
	 * @return array list of localized language names.
	 */
	function getLanguages()
	{
		return $this->simplify($this->findInfo('Languages',true));
	}

	/**
	 * Get a list of scripts in the language of the localized version.
	 * @return array list of localized script names.
	 */
	function getScripts()
	{
		return $this->simplify($this->findInfo('Scripts',true));
	}

	/**
	 * Get a list of timezones in the language of the localized version.
	 * @return array list of localized timezones.
	 */
	function getTimeZones()
	{
		return $this->simplify($this->findInfo('zoneStrings',true));
	}
}

