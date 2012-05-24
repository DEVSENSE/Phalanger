<?php
/**
 * TMssqlTableInfo class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TMssqlTableInfo.php 1861 2007-04-12 08:05:03Z wei $
 * @package System.Data.Common.Mssql
 */

/**
 * Loads the base TDbTableInfo class and TMssqlTableColumn class.
 */
Prado::using('System.Data.Common.TDbTableInfo');
Prado::using('System.Data.Common.Mssql.TMssqlTableColumn');

/**
 * TMssqlTableInfo class provides additional table information for Mssql database.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TMssqlTableInfo.php 1861 2007-04-12 08:05:03Z wei $
 * @package System.Data.Common.Mssql
 * @since 3.1
 */
class TMssqlTableInfo extends TDbTableInfo
{
	/**
	 * @return string name of the schema this column belongs to.
	 */
	public function getSchemaName()
	{
		return $this->getInfo('SchemaName');
	}

	/**
	 * @return string catalog name (database name)
	 */
	public function getCatalogName()
	{
		return $this->getInfo('CatalogName');
	}

	/**
	 * @return string full name of the table, database dependent.
	 */
	public function getTableFullName()
	{
		//MSSQL alway returns the catalog, schem and table names.
		return '['.$this->getCatalogName().'].['.$this->getSchemaName().'].['.$this->getTableName().']';
	}

	/**
	 * @param TDbConnection database connection.
	 * @return TDbCommandBuilder new command builder
	 */
	public function createCommandBuilder($connection)
	{
		Prado::using('System.Data.Common.Mssql.TMssqlCommandBuilder');
		return new TMssqlCommandBuilder($connection,$this);
	}
}

