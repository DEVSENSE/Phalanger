<?php
/**
 * TSqliteMetaData class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqliteMetaData.php 1861 2007-04-12 08:05:03Z wei $
 * @package System.Data.Common.Sqlite
 */

/**
 * Load the base TDbMetaData class.
 */
Prado::using('System.Data.Common.TDbMetaData');
Prado::using('System.Data.Common.Sqlite.TSqliteTableInfo');

/**
 * TSqliteMetaData loads SQLite database table and column information.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqliteMetaData.php 1861 2007-04-12 08:05:03Z wei $
 * @package System.Data.Commom.Sqlite
 * @since 3.1
 */
class TSqliteMetaData extends TDbMetaData
{
	/**
	 * @return string TDbTableInfo class name.
	 */
	protected function getTableInfoClass()
	{
		return 'TSqliteTableInfo';
	}

	/**
	 * Get the column definitions for given table.
	 * @param string table name.
	 * @return TPgsqlTableInfo table information.
	 */
	protected function createTableInfo($tableName)
	{
		$tableName = str_replace("'",'',$tableName);
		$this->getDbConnection()->setActive(true);
		$table = $this->getDbConnection()->quoteString($tableName);
		$sql = "PRAGMA table_info({$table})";
		$command = $this->getDbConnection()->createCommand($sql);
		$foreign = $this->getForeignKeys($table);
		$index=0;
		$columns=array();
		$primary=array();
		foreach($command->query() as $col)
		{
			$col['index'] = $index++;
			$column = $this->processColumn($col, $foreign);
			$columns[$col['name']] = $column;
			if($column->getIsPrimaryKey())
				$primary[] = $col['name'];
		}
		$info['TableName'] = $tableName;
		if($this->getIsView($tableName))
			$info['IsView'] = true;
		if(count($columns)===0)
			throw new TDbException('dbmetadata_invalid_table_view', $tableName);
		$class = $this->getTableInfoClass();
		$tableInfo = new $class($info,$primary,$foreign);
		$tableInfo->getColumns()->copyFrom($columns);
		return $tableInfo;
	}

	/**
	 * @param string table name.
	 * @return boolean true if the table is a view.
	 */
	protected function getIsView($tableName)
	{
		$sql = 'SELECT count(*) FROM sqlite_master WHERE type="view" AND name= :table';
		$this->getDbConnection()->setActive(true);
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $tableName);
		return intval($command->queryScalar()) === 1;
	}

	/**
	 * @param array column information.
	 * @param array foreign key details.
	 * @return TSqliteTableColumn column details.
	 */
	protected function processColumn($col, $foreign)
	{
		$columnId = $col['name']; //use column name as column Id

		$info['ColumnName'] = '"'.$columnId.'"'; //quote the column names!
		$info['ColumnId'] = $columnId;
		$info['ColumnIndex'] = $col['index'];

		if($col['notnull']!=='99')
			$info['AllowNull'] = true;

		if($col['pk']==='1')
			$info['IsPrimaryKey'] = true;
		if($this->isForeignKeyColumn($columnId, $foreign))
			$info['IsForeignKey'] = true;

		if($col['dflt_value']!==null)
			$info['DefaultValue'] = $col['dflt_value'];

		$type = strtolower($col['type']);
		$info['AutoIncrement'] = $type==='integer' && $col['pk']==='1';

		$info['DbType'] = $type;
		$match=array();
		if(is_int($pos=strpos($type, '(')) && preg_match('/\((.*)\)/', $type, $match))
		{
			$ps = explode(',', $match[1]);
			if(count($ps)===2)
			{
				$info['NumericPrecision'] = intval($ps[0]);
				$info['NumericScale'] = intval($ps[1]);
			}
			else
				$info['ColumnSize']=intval($match[1]);
			$info['DbType'] = substr($type,0,$pos);
		}

		return new TSqliteTableColumn($info);
	}

	/**
	 *
	 *
	 * @param string quoted table name.
	 * @return array foreign key details.
	 */
	protected function getForeignKeys($table)
	{
		$sql = "PRAGMA foreign_key_list({$table})";
		$command = $this->getDbConnection()->createCommand($sql);
		$fkeys = array();
		foreach($command->query() as $col)
		{
			$fkeys[$col['table']]['keys'][$col['from']] = $col['to'];
			$fkeys[$col['table']]['table'] = $col['table'];
		}
		return count($fkeys) > 0 ? array_values($fkeys) : $fkeys;
	}

	/**
	 * @param string column name.
	 * @param array foreign key column names.
	 * @return boolean true if column is a foreign key.
	 */
	protected function isForeignKeyColumn($columnId, $foreign)
	{
		foreach($foreign as $fk)
		{
			if(in_array($columnId, array_keys($fk['keys'])))
				return true;
		}
		return false;
	}
}

/**

CREATE TABLE foo
(
	id INTEGER NOT NULL PRIMARY KEY,
	id2 CHAR(2)
);

CREATE TABLE bar
(
	id INTEGER NOT NULL PRIMARY KEY,
	foo_id INTEGER
		CONSTRAINT fk_foo_id REFERENCES foo(id) ON DELETE CASCADE
);
*/

