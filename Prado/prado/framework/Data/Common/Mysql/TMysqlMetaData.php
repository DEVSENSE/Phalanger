<?php
/**
 * TMysqlMetaData class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TMysqlMetaData.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.Common.Mysql
 */

/**
 * Load the base TDbMetaData class.
 */
Prado::using('System.Data.Common.TDbMetaData');
Prado::using('System.Data.Common.Mysql.TMysqlTableInfo');

/**
 * TMysqlMetaData loads Mysql version 4.1.x and 5.x database table and column information.
 *
 * For Mysql version 4.1.x, PHP 5.1.3 or later is required.
 * See http://netevil.org/node.php?nid=795&SC=1
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TMysqlMetaData.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.Commom.Sqlite
 * @since 3.1
 */
class TMysqlMetaData extends TDbMetaData
{
	private $_serverVersion=0;

	/**
	 * @return string TDbTableInfo class name.
	 */
	protected function getTableInfoClass()
	{
		return 'TMysqlTableInfo';
	}

	/**
	 * Get the column definitions for given table.
	 * @param string table name.
	 * @return TMysqlTableInfo table information.
	 */
	protected function createTableInfo($table)
	{
		list($schemaName,$tableName) = $this->getSchemaTableName($table);
		$find = $schemaName===null ? "`{$tableName}`" : "`{$schemaName}`.`{$tableName}`";
		$this->getDbConnection()->setActive(true);
		$sql = "SHOW FULL FIELDS FROM {$find}";
		$command = $this->getDbConnection()->createCommand($sql);
		$tableInfo = $this->createNewTableInfo($table);
		$index=0;
		foreach($command->query() as $col)
		{
			$col['index'] = $index++;
			$this->processColumn($tableInfo,$col);
		}
		if($index===0)
			throw new TDbException('dbmetadata_invalid_table_view', $table);
		return $tableInfo;
	}

	/**
	 * @return float server version.
	 */
	protected function getServerVersion()
	{
		if(!$this->_serverVersion)
		{
			$version = $this->getDbConnection()->getAttribute(PDO::ATTR_SERVER_VERSION);
			$digits=array();
			preg_match('/(\d+)\.(\d+)\.(\d+)/', $version, $digits);
			$this->_serverVersion=floatval($digits[1].'.'.$digits[2].$digits[3]);
		}
		return $this->_serverVersion;
	}

	/**
	 * @param TMysqlTableInfo table information.
	 * @param array column information.
	 */
	protected function processColumn($tableInfo, $col)
	{
		$columnId = $col['Field'];

		$info['ColumnName'] = "`$columnId`"; //quote the column names!
		$info['ColumnId'] = $columnId;
		$info['ColumnIndex'] = $col['index'];
		if($col['Null']==='YES')
			$info['AllowNull'] = true;
		if(is_int(strpos(strtolower($col['Extra']), 'auto_increment')))
			$info['AutoIncrement']=true;
		if($col['Default']!=="")
			$info['DefaultValue'] = $col['Default'];

		if($col['Key']==='PRI' || in_array($columnId, $tableInfo->getPrimaryKeys()))
			$info['IsPrimaryKey'] = true;
		if($this->isForeignKeyColumn($columnId, $tableInfo))
			$info['IsForeignKey'] = true;

		$info['DbType'] = $col['Type'];
		$match=array();
		//find SET/ENUM values, column size, precision, and scale
		if(preg_match('/\((.*)\)/', $col['Type'], $match))
		{
			$info['DbType']= preg_replace('/\(.*\)/', '', $col['Type']);

			//find SET/ENUM values
			if($this->isEnumSetType($info['DbType']))
				$info['DbTypeValues'] = preg_split("/[',]/S", $match[1], -1, PREG_SPLIT_NO_EMPTY);

			//find column size, precision and scale
			$pscale = array();
			if(preg_match('/(\d+)(?:,(\d+))?+/', $match[1], $pscale))
			{
				if($this->isPrecisionType($info['DbType']))
				{
					$info['NumericPrecision'] = intval($pscale[1]);
					if(count($pscale) > 2)
						$info['NumericScale'] = intval($pscale[2]);
				}
				else
					$info['ColumnSize'] = intval($pscale[1]);
			}
		}

		$tableInfo->Columns[$columnId] = new TMysqlTableColumn($info);
	}

	/**
	 * @return boolean true if column type if "numeric", "interval" or begins with "time".
	 */
	protected function isPrecisionType($type)
	{
		$type = strtolower(trim($type));
		return $type==='decimal' || $type==='dec'
				|| $type==='float' || $type==='double'
				|| $type==='double precision' || $type==='real';
	}

	/**
	 * @return boolean true if column type if "enum" or "set".
	 */
	protected function isEnumSetType($type)
	{
		$type = strtolower(trim($type));
		return $type==='set' || $type==='enum';
	}

	/**
	 * @param string table name, may be quoted with back-ticks and may contain database name.
	 * @return array tuple ($schema,$table), $schema may be null.
	 * @throws TDbException when table name contains invalid identifier bytes.
	 */
	protected function getSchemaTableName($table)
	{
		//remove the back ticks and separate out the "database.table"
		$result = explode('.', str_replace('`', '', $table));
		foreach($result as $name)
		{
			if(!$this->isValidIdentifier($name))
			{
				$ref = 'http://dev.mysql.com/doc/refman/5.0/en/identifiers.html';
				throw new TDbException('dbcommon_invalid_identifier_name', $table, $ref);
			}
		}
		return count($result) > 1 ? $result : array(null, $result[0]);
	}

	/**
	 * http://dev.mysql.com/doc/refman/5.0/en/identifiers.html
	 * @param string identifier name
	 * @param boolean true if valid identifier.
	 */
	protected function isValidIdentifier($name)
	{
		return !preg_match('#/|\\|.|\x00|\xFF#', $name);
	}

	/**
	 * @param string table schema name
	 * @param string table name.
	 * @return TMysqlTableInfo
	 */
	protected function createNewTableInfo($table)
	{
		list($schemaName,$tableName) = $this->getSchemaTableName($table);
		$info['SchemaName'] = $schemaName;
		$info['TableName'] = $tableName;
		if($this->getIsView($schemaName,$tableName))
			$info['IsView'] = true;
		list($primary, $foreign) = $this->getConstraintKeys($schemaName, $tableName);
		$class = $this->getTableInfoClass();
		return new $class($info,$primary,$foreign);
	}

	/**
	 * For MySQL version 5.0.1 or later we can use SHOW FULL TABLES
	 * http://dev.mysql.com/doc/refman/5.0/en/show-tables.html
	 *
	 * For MySQL version 5.0.1 or ealier, this always return false.
	 * @param string database name, null to use default connection database.
	 * @param string table or view name.
	 * @return boolean true if is view, false otherwise.
	 * @throws TDbException if table or view does not exist.
	 */
	protected function getIsView($schemaName,$tableName)
	{
		if($this->getServerVersion()<5.01)
			return false;
		if($schemaName!==null)
			$sql = "SHOW FULL TABLES FROM `{$schemaName}` LIKE :table";
		else
			$sql = "SHOW FULL TABLES LIKE :table";

		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $tableName);
		try
		{
			return count($result = $command->queryRow()) > 0 && $result['Table_type']==='VIEW';
		}
		catch(TDbException $e)
		{
			$table = $schemaName===null?$tableName:$schemaName.'.'.$tableName;
			throw new TDbException('dbcommon_invalid_table_name',$table,$e->getMessage());
		}
	}

	/**
	 * Gets the primary and foreign key column details for the given table.
	 * @param string schema name
	 * @param string table name.
	 * @return array tuple ($primary, $foreign)
	 */
	protected function getConstraintKeys($schemaName, $tableName)
	{
		$table = $schemaName===null ? "`{$tableName}`" : "`{$schemaName}`.`{$tableName}`";
		$sql = "SHOW INDEX FROM {$table}";
		$command = $this->getDbConnection()->createCommand($sql);
		$primary = array();
		foreach($command->query() as $row)
		{
			if($row['Key_name']==='PRIMARY')
				$primary[] = $row['Column_name'];
		}
        // MySQL version was increased to >=5.1.21 instead of 5.x
        // due to a MySQL bug (http://bugs.mysql.com/bug.php?id=19588)
		if($this->getServerVersion() >= 5.121)
			$foreign = $this->getForeignConstraints($schemaName,$tableName);
		else
			$foreign = $this->findForeignConstraints($schemaName,$tableName);
		return array($primary,$foreign);
	}

	/**
	 * Gets foreign relationship constraint keys and table name
	 * @param string database name
	 * @param string table name
	 * @return array foreign relationship table name and keys.
	 */
	protected function getForeignConstraints($schemaName, $tableName)
	{
		$andSchema = $schemaName !== null ? 'AND TABLE_SCHEMA LIKE :schema' : '';
		$sql = <<<EOD
			SELECT
				CONSTRAINT_NAME as con,
				COLUMN_NAME as col,
				REFERENCED_TABLE_SCHEMA as fkschema,
				REFERENCED_TABLE_NAME as fktable,
				REFERENCED_COLUMN_NAME as fkcol
			FROM
				 `INFORMATION_SCHEMA`.`KEY_COLUMN_USAGE`
			WHERE
				REFERENCED_TABLE_NAME IS NOT NULL
				AND TABLE_NAME LIKE :table
				$andSchema
EOD;
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $tableName);
		if($schemaName!==null)
			$command->bindValue(':schema', $schemaName);
		$fkeys=array();
		foreach($command->query() as $col)
		{
			$fkeys[$col['con']]['keys'][$col['col']] = $col['fkcol'];
			$fkeys[$col['con']]['table'] = $col['fktable'];
		}
		return count($fkeys) > 0 ? array_values($fkeys) : $fkeys;
	}

	/**
	 * @param string database name
	 * @param string table name
	 * @return string SQL command to create the table.
	 * @throws TDbException if PHP version is less than 5.1.3
	 */
	protected function getShowCreateTable($schemaName, $tableName)
	{
		if(version_compare(PHP_VERSION,'5.1.3','<'))
			throw new TDbException('dbmetadata_requires_php_version', 'Mysql 4.1.x', '5.1.3');

		//See http://netevil.org/node.php?nid=795&SC=1
		$this->getDbConnection()->setAttribute(PDO::ATTR_EMULATE_PREPARES, true);
		if($schemaName!==null)
			$sql = "SHOW CREATE TABLE `{$schemaName}`.`{$tableName}`";
		else
			$sql = "SHOW CREATE TABLE `{$tableName}`";
		$command = $this->getDbConnection()->createCommand($sql);
		$result = $command->queryRow();
		return isset($result['Create Table']) ? $result['Create Table'] : (isset($result['Create View']) ? $result['Create View'] : '');
	}

	/**
	 * Extract foreign key constraints by extracting the contraints from SHOW CREATE TABLE result.
	 * @param string database name
	 * @param string table name
	 * @return array foreign relationship table name and keys.
	 */
	protected function findForeignConstraints($schemaName, $tableName)
	{
		$sql = $this->getShowCreateTable($schemaName, $tableName);
		$matches =array();
		$regexp = '/FOREIGN KEY\s+\(([^\)]+)\)\s+REFERENCES\s+`?([^`]+)`?\s\(([^\)]+)\)/mi';
		preg_match_all($regexp,$sql,$matches,PREG_SET_ORDER);
		$foreign = array();
		foreach($matches as $match)
		{
			$fields = array_map('trim',explode(',',str_replace('`','',$match[1])));
			$fk_fields = array_map('trim',explode(',',str_replace('`','',$match[3])));
			$keys=array();
			foreach($fields as $k=>$v)
				$keys[$v] = $fk_fields[$k];
			$foreign[] = array('keys' => $keys, 'table' => trim($match[2]));
		}
		return $foreign;
	}

	/**
	 * @param string column name.
	 * @param TPgsqlTableInfo table information.
	 * @return boolean true if column is a foreign key.
	 */
	protected function isForeignKeyColumn($columnId, $tableInfo)
	{
		foreach($tableInfo->getForeignKeys() as $fk)
		{
			if(in_array($columnId, array_keys($fk['keys'])))
				return true;
		}
		return false;
	}
}

