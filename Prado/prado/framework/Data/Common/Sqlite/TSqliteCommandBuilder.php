<?php
/**
 * TSqliteCommandBuilder class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbCommandBuilder.php 1863 2007-04-12 12:43:49Z wei $
 * @package System.Data.Common
 */

Prado::using('System.Data.Common.TDbCommandBuilder');

/**
 * TSqliteCommandBuilder provides specifics methods to create limit/offset query commands
 * for Sqlite database.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDbCommandBuilder.php 1863 2007-04-12 12:43:49Z wei $
 * @package System.Data.Common
 * @since 3.1
 */
class TSqliteCommandBuilder extends TDbCommandBuilder
{
	/**
	 * Alters the sql to apply $limit and $offset.
	 * @param string SQL query string.
	 * @param integer maximum number of rows, -1 to ignore limit.
	 * @param integer row offset, -1 to ignore offset.
	 * @return string SQL with limit and offset.
	 */
	public function applyLimitOffset($sql, $limit=-1, $offset=-1)
	{
		$limit = $limit!==null ? intval($limit) : -1;
		$offset = $offset!==null ? intval($offset) : -1;
		if($limit > 0 || $offset > 0)
		{
			$limitStr = ' LIMIT '.$limit;
			$offsetStr = $offset >= 0 ? ' OFFSET '.$offset : '';
			return $sql.$limitStr.$offsetStr;
		}
		else
			return $sql;
	}
}

