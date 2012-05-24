<?php
/**
 * TSqliteTableInfo class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqliteTableInfo.php 1861 2007-04-12 08:05:03Z wei $
 * @package System.Data.Common.Sqlite
 */

/**
 * Loads the base TDbTableInfo class and TSqliteTableColumn class.
 */
Prado::using('System.Data.Common.TDbTableInfo');
Prado::using('System.Data.Common.Sqlite.TSqliteTableColumn');

/**
 * TSqliteTableInfo class provides additional table information for PostgreSQL database.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqliteTableInfo.php 1861 2007-04-12 08:05:03Z wei $
 * @package System.Data.Common.Sqlite
 * @since 3.1
 */
class TSqliteTableInfo extends TDbTableInfo
{
	/**
	 * @param TDbConnection database connection.
	 * @return TDbCommandBuilder new command builder
	 */
	public function createCommandBuilder($connection)
	{
		Prado::using('System.Data.Common.Sqlite.TSqliteCommandBuilder');
		return new TSqliteCommandBuilder($connection,$this);
	}

	/**
	 * @return string full name of the table, database dependent.
	 */
	public function getTableFullName()
	{
		return "'".$this->getTableName()."'";
	}
}

