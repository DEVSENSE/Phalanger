<?php
/**
 * TMssqlMetaData class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPgsqlMetaData.php 1866 2007-04-14 05:02:29Z wei $
 * @package System.Data.Common.Pgsql
 */

/**
 * Load the base TDbMetaData class.
 */
Prado::using('System.Data.Common.TDbMetaData');
Prado::using('System.Data.Common.Mssql.TMssqlTableInfo');

/**
 * TMssqlMetaData loads MSSQL database table and column information.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TPgsqlMetaData.php 1866 2007-04-14 05:02:29Z wei $
 * @package System.Data.Common.Mssql
 * @since 3.1
 */
class TMssqlMetaData extends TDbMetaData
{
	/**
	 * @return string TDbTableInfo class name.
	 */
	protected function getTableInfoClass()
	{
		return 'TMssqlTableInfo';
	}

	/**
	 * Get the column definitions for given table.
	 * @param string table name.
	 * @return TMssqlTableInfo table information.
	 */
	protected function createTableInfo($table)
	{
		list($catalogName,$schemaName,$tableName) = $this->getCatalogSchemaTableName($table);
		$this->getDbConnection()->setActive(true);
		$sql = <<<EOD
				SELECT t.*,
                         c.*,
					columnproperty(object_id(c.table_schema + '.' + c.table_name), c.column_name,'IsIdentity') as IsIdentity
                    FROM INFORMATION_SCHEMA.TABLES t,
                         INFORMATION_SCHEMA.COLUMNS c
                   WHERE t.table_name = c.table_name
                     AND t.table_name = :table
EOD;
		if($schemaName!==null)
			$sql .= ' AND t.table_schema = :schema';
		if($catalogName!==null)
			$sql .= ' AND t.table_catalog = :catalog';

		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $tableName);
		if($schemaName!==null)
			$command->bindValue(':schema', $schemaName);
		if($catalogName!==null)
			$command->bindValue(':catalog', $catalogName);

		$tableInfo=null;
		foreach($command->query() as $col)
		{
			if($tableInfo===null)
				$tableInfo = $this->createNewTableInfo($col);
			$this->processColumn($tableInfo,$col);
		}
		if($tableInfo===null)
			throw new TDbException('dbmetadata_invalid_table_view', $table);
		return $tableInfo;
	}

	/**
	 * @param string table name
	 * @return array tuple($catalogName,$schemaName,$tableName)
	 */
	protected function getCatalogSchemaTableName($table)
	{
		//remove possible delimiters
		$result = explode('.', preg_replace('/\[|\]|"/', '', $table));
		if(count($result)===1)
			return array(null,null,$result[0]);
		if(count($result)===2)
			return array(null,$result[0],$result[1]);
		if(count($result)>2)
			return array($result[0],$result[1],$result[2]);
	}

	/**
	 * @param TMssqlTableInfo table information.
	 * @param array column information.
	 */
	protected function processColumn($tableInfo, $col)
	{
		$columnId = $col['COLUMN_NAME'];

		$info['ColumnName'] = "[$columnId]"; //quote the column names!
		$info['ColumnId'] = $columnId;
		$info['ColumnIndex'] = intval($col['ORDINAL_POSITION'])-1; //zero-based index
		if($col['IS_NULLABLE']!=='NO')
			$info['AllowNull'] = true;
		if($col['COLUMN_DEFAULT']!==null)
			$info['DefaultValue'] = $col['COLUMN_DEFAULT'];

		if(in_array($columnId, $tableInfo->getPrimaryKeys()))
			$info['IsPrimaryKey'] = true;
		if($this->isForeignKeyColumn($columnId, $tableInfo))
			$info['IsForeignKey'] = true;

		if($col['IsIdentity']==='1')
			$info['AutoIncrement'] = true;
		$info['DbType'] = $col['DATA_TYPE'];
		if($col['CHARACTER_MAXIMUM_LENGTH']!==null)
			$info['ColumnSize'] = intval($col['CHARACTER_MAXIMUM_LENGTH']);
		if($col['NUMERIC_PRECISION'] !== null)
			$info['NumericPrecision'] = intval($col['NUMERIC_PRECISION']);
		if($col['NUMERIC_SCALE']!==null)
			$info['NumericScale'] = intval($col['NUMERIC_SCALE']);
		$tableInfo->Columns[$columnId] = new TMssqlTableColumn($info);
	}

	/**
	 * @param string table schema name
	 * @param string table name.
	 * @return TMssqlTableInfo
	 */
	protected function createNewTableInfo($col)
	{
		$info['CatalogName'] = $col['TABLE_CATALOG'];
		$info['SchemaName'] = $col['TABLE_SCHEMA'];
		$info['TableName'] = $col['TABLE_NAME'];
		if($col['TABLE_TYPE']==='VIEW')
			$info['IsView'] = true;
		list($primary, $foreign) = $this->getConstraintKeys($col);
		$class = $this->getTableInfoClass();
		return new $class($info,$primary,$foreign);
	}

	/**
	 * Gets the primary and foreign key column details for the given table.
	 * @param string schema name
	 * @param string table name.
	 * @return array tuple ($primary, $foreign)
	 */
	protected function getConstraintKeys($col)
	{
		$sql = <<<EOD
		SELECT k.column_name field_name
		    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
		    LEFT JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS c
		      ON k.table_name = c.table_name
		     AND k.constraint_name = c.constraint_name
		   WHERE k.constraint_catalog = DB_NAME()
		AND
			c.constraint_type ='PRIMARY KEY'
		    AND k.table_name = :table
EOD;
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $col['TABLE_NAME']);
		$primary = array();
		foreach($command->query()->readAll() as $field)
			$primary[] = $field['field_name'];
		$foreign = $this->getForeignConstraints($col);
		return array($primary,$foreign);
	}

	/**
	 * Gets foreign relationship constraint keys and table name
	 * @param string database name
	 * @param string table name
	 * @return array foreign relationship table name and keys.
	 */
	protected function getForeignConstraints($col)
	{
		//From http://msdn2.microsoft.com/en-us/library/aa175805(SQL.80).aspx
		$sql = <<<EOD
		SELECT
		     KCU1.CONSTRAINT_NAME AS 'FK_CONSTRAINT_NAME'
		   , KCU1.TABLE_NAME AS 'FK_TABLE_NAME'
		   , KCU1.COLUMN_NAME AS 'FK_COLUMN_NAME'
		   , KCU1.ORDINAL_POSITION AS 'FK_ORDINAL_POSITION'
		   , KCU2.CONSTRAINT_NAME AS 'UQ_CONSTRAINT_NAME'
		   , KCU2.TABLE_NAME AS 'UQ_TABLE_NAME'
		   , KCU2.COLUMN_NAME AS 'UQ_COLUMN_NAME'
		   , KCU2.ORDINAL_POSITION AS 'UQ_ORDINAL_POSITION'
		FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC
		JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU1
		ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG
		   AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA
		   AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME
		JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2
		ON KCU2.CONSTRAINT_CATALOG =
		RC.UNIQUE_CONSTRAINT_CATALOG
		   AND KCU2.CONSTRAINT_SCHEMA =
		RC.UNIQUE_CONSTRAINT_SCHEMA
		   AND KCU2.CONSTRAINT_NAME =
		RC.UNIQUE_CONSTRAINT_NAME
		   AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION
		WHERE KCU1.TABLE_NAME = :table
EOD;
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $col['TABLE_NAME']);
		$fkeys=array();
		$catalogSchema = "[{$col['TABLE_CATALOG']}].[{$col['TABLE_SCHEMA']}]";
		foreach($command->query() as $info)
		{
			$fkeys[$info['FK_CONSTRAINT_NAME']]['keys'][$info['FK_COLUMN_NAME']] = $info['UQ_COLUMN_NAME'];
			$fkeys[$info['FK_CONSTRAINT_NAME']]['table'] = $info['UQ_TABLE_NAME'];
		}
		return count($fkeys) > 0 ? array_values($fkeys) : $fkeys;
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

