<?php
/**
 * TSimpleDynamicParser class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSimpleDynamicParser.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Configuration
 */

/**
 * TSimpleDynamicParser finds place holders $name$ in the sql text and replaces
 * it with a TSimpleDynamicParser::DYNAMIC_TOKEN.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSimpleDynamicParser.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSimpleDynamicParser
{
	const PARAMETER_TOKEN_REGEXP = '/\$([^\$]+)\$/';
	const DYNAMIC_TOKEN = '`!`';

	/**
	 * Parse the sql text for dynamic place holders of the form $name$.
	 * @param string Sql text.
	 * @return array name value pairs 'sql' and 'parameters'.
	 */
	public function parse($sqlText)
	{
		$matches = array();
		$mappings = array();
		preg_match_all(self::PARAMETER_TOKEN_REGEXP, $sqlText, $matches);
		for($i = 0, $k=count($matches[1]); $i<$k; $i++)
		{
			$mappings[] = $matches[1][$i];
			$sqlText = str_replace($matches[0][$i], self::DYNAMIC_TOKEN, $sqlText);
		}
		return array('sql'=>$sqlText, 'parameters'=>$mappings);
	}
}

