<?php
/**
 * TPgsqlTableInfo class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPgsqlTableInfo.php 2654 2009-05-14 07:34:16Z godzilla80@gmx.net $
 * @package System.Data.Common.Pgsql
 */

/**
 * Loads the base TDbTableInfo class and TPgsqlTableColumn class.
 */
Prado::using('System.Data.Common.TDbTableInfo');
Prado::using('System.Data.Common.Pgsql.TPgsqlTableColumn');

/**
 * TPgsqlTableInfo class provides additional table information for PostgreSQL database.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TPgsqlTableInfo.php 2654 2009-05-14 07:34:16Z godzilla80@gmx.net $
 * @package System.Data.Common.Pgsql
 * @since 3.1
 */
class TPgsqlTableInfo extends TDbTableInfo
{
	/**
	 * @return string name of the schema this column belongs to.
	 */
	public function getSchemaName()
	{
		return $this->getInfo('SchemaName');
	}

	/**
	 * @return string full name of the table, database dependent.
	 */
	public function getTableFullName()
	{
		if(($schema=$this->getSchemaName())!==null)
			return $schema.'.'.$this->getTableName();
		else
			return $this->getTableName();
	}

	/**
	 * @param TDbConnection database connection.
	 * @return TDbCommandBuilder new command builder
	 */
	public function createCommandBuilder($connection)
	{
		Prado::using('System.Data.Common.Pgsql.TPgsqlCommandBuilder');
		return new TPgsqlCommandBuilder($connection,$this);
	}
}

