<?php
/**
 * TDbMetaData class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbMetaData.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 */

/**
 * TDbMetaData is the base class for retrieving metadata information, such as
 * table and columns information, from a database connection.
 *
 * Use the {@link getTableInfo} method to retrieve a table information.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDbMetaData.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 * @since 3.1
 */
abstract class TDbMetaData extends TComponent
{
	private $_tableInfoCache=array();
	private $_connection;

	/**
	 * @param TDbConnection database connection.
	 */
	public function __construct($conn)
	{
		$this->_connection=$conn;
	}

	/**
	 * @return TDbConnection database connection.
	 */
	public function getDbConnection()
	{
		return $this->_connection;
	}

	/**
	 * Obtain database specific TDbMetaData class using the driver name of the database connection.
	 * @param TDbConnection database connection.
	 * @return TDbMetaData database specific TDbMetaData.
	 */
	public static function getInstance($conn)
	{
		$conn->setActive(true); //must be connected before retrieving driver name
		$driver = $conn->getDriverName();
		switch(strtolower($driver))
		{
			case 'pgsql':
				Prado::using('System.Data.Common.Pgsql.TPgsqlMetaData');
				return new TPgsqlMetaData($conn);
			case 'mysqli':
			case 'mysql':
				Prado::using('System.Data.Common.Mysql.TMysqlMetaData');
				return new TMysqlMetaData($conn);
			case 'sqlite': //sqlite 3
			case 'sqlite2': //sqlite 2
				Prado::using('System.Data.Common.Sqlite.TSqliteMetaData');
				return new TSqliteMetaData($conn);
			case 'mssql': // Mssql driver on windows hosts
			case 'dblib': // dblib drivers on linux (and maybe others os) hosts
				Prado::using('System.Data.Common.Mssql.TMssqlMetaData');
				return new TMssqlMetaData($conn);
			case 'oci':
				Prado::using('System.Data.Common.Oracle.TOracleMetaData');
				return new TOracleMetaData($conn);
//			case 'ibm':
//				Prado::using('System.Data.Common.IbmDb2.TIbmDb2MetaData');
//				return new TIbmDb2MetaData($conn);
			default:
				throw new TDbException('ar_invalid_database_driver',$driver);
		}
	}

	/**
	 * Obtains table meta data information for the current connection and given table name.
	 * @param string table or view name
	 * @return TDbTableInfo table information.
	 */
	public function getTableInfo($tableName=null)
	{
		$key = $tableName===null?$this->getDbConnection()->getConnectionString():$tableName;
		if(!isset($this->_tableInfoCache[$key]))
		{
			$class = $this->getTableInfoClass();
			$tableInfo = $tableName===null ? new $class : $this->createTableInfo($tableName);
			$this->_tableInfoCache[$key] = $tableInfo;
		}
		return $this->_tableInfoCache[$key];
	}

	/**
	 * Creates a command builder for a given table name.
	 * @param string table name.
	 * @return TDbCommandBuilder command builder instance for the given table.
	 */
	public function createCommandBuilder($tableName=null)
	{
		return $this->getTableInfo($tableName)->createCommandBuilder($this->getDbConnection());
	}

	/**
	 * This method should be implemented by decendent classes.
	 * @return TDbTableInfo driver dependent create builder.
	 */
	abstract protected function createTableInfo($tableName);

	/**
	 * @return string TDbTableInfo class name.
	 */
	protected function getTableInfoClass()
	{
		return 'TDbTableInfo';
	}
}

