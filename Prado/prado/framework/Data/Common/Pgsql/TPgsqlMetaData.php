<?php
/**
 * TPgsqlMetaData class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPgsqlMetaData.php 2641 2009-04-26 06:22:19Z godzilla80@gmx.net $
 * @package System.Data.Common.Pgsql
 */

/**
 * Load the base TDbMetaData class.
 */
Prado::using('System.Data.Common.TDbMetaData');
Prado::using('System.Data.Common.Pgsql.TPgsqlTableInfo');

/**
 * TPgsqlMetaData loads PostgreSQL database table and column information.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TPgsqlMetaData.php 2641 2009-04-26 06:22:19Z godzilla80@gmx.net $
 * @package System.Data.Common.Pgsql
 * @since 3.1
 */
class TPgsqlMetaData extends TDbMetaData
{
	private $_defaultSchema = 'public';

	/**
	 * @return string TDbTableInfo class name.
	 */
	protected function getTableInfoClass()
	{
		return 'TPgsqlTableInfo';
	}

	/**
	 * @param string default schema.
	 */
	public function setDefaultSchema($schema)
	{
		$this->_defaultSchema=$schema;
	}

	/**
	 * @return string default schema.
	 */
	public function getDefaultSchema()
	{
		return $this->_defaultSchema;
	}

	/**
	 * @param string table name with optional schema name prefix, uses default schema name prefix is not provided.
	 * @return array tuple as ($schemaName,$tableName)
	 */
	protected function getSchemaTableName($table)
	{
		if(count($parts= explode('.', str_replace('"','',$table))) > 1)
			return array($parts[0], $parts[1]);
		else
			return array($this->getDefaultSchema(),$parts[0]);
	}

	/**
	 * Get the column definitions for given table.
	 * @param string table name.
	 * @return TPgsqlTableInfo table information.
	 */
	protected function createTableInfo($table)
	{
		list($schemaName,$tableName) = $this->getSchemaTableName($table);

		// This query is made much more complex by the addition of the 'attisserial' field.
		// The subquery to get that field checks to see if there is an internally dependent
		// sequence on the field.
		$sql =
<<<EOD
		SELECT
			a.attname,
			pg_catalog.format_type(a.atttypid, a.atttypmod) as type,
			a.atttypmod,
			a.attnotnull, a.atthasdef, adef.adsrc,
			(
				SELECT 1 FROM pg_catalog.pg_depend pd, pg_catalog.pg_class pc
				WHERE pd.objid=pc.oid
				AND pd.classid=pc.tableoid
				AND pd.refclassid=pc.tableoid
				AND pd.refobjid=a.attrelid
				AND pd.refobjsubid=a.attnum
				AND pd.deptype='i'
				AND pc.relkind='S'
			) IS NOT NULL AS attisserial

		FROM
			pg_catalog.pg_attribute a LEFT JOIN pg_catalog.pg_attrdef adef
			ON a.attrelid=adef.adrelid
			AND a.attnum=adef.adnum
			LEFT JOIN pg_catalog.pg_type t ON a.atttypid=t.oid
		WHERE
			a.attrelid = (SELECT oid FROM pg_catalog.pg_class WHERE relname=:table
				AND relnamespace = (SELECT oid FROM pg_catalog.pg_namespace WHERE
				nspname = :schema))
			AND a.attnum > 0 AND NOT a.attisdropped
		ORDER BY a.attnum
EOD;
		$this->getDbConnection()->setActive(true);
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $tableName);
		$command->bindValue(':schema', $schemaName);
		$tableInfo = $this->createNewTableInfo($schemaName, $tableName);
		$index=0;
		foreach($command->query() as $col)
		{
			$col['index'] = $index++;
			$this->processColumn($tableInfo, $col);
		}
		if($index===0)
			throw new TDbException('dbmetadata_invalid_table_view', $table);
		return $tableInfo;
	}

	/**
	 * @param string table schema name
	 * @param string table name.
	 * @return TPgsqlTableInfo
	 */
	protected function createNewTableInfo($schemaName,$tableName)
	{
		$info['SchemaName'] = $this->assertIdentifier($schemaName);
		$info['TableName'] = $this->assertIdentifier($tableName);
		if($this->getIsView($schemaName,$tableName))
			$info['IsView'] = true;
		list($primary, $foreign) = $this->getConstraintKeys($schemaName, $tableName);
		$class = $this->getTableInfoClass();
		return new $class($info,$primary,$foreign);
	}

	/**
	 * @param string table name, schema name or column name.
	 * @return string a valid identifier.
	 * @throws TDbException when table name contains a double quote (").
	 */
	protected function assertIdentifier($name)
	{
		if(strpos($name, '"')!==false)
		{
			$ref = 'http://www.postgresql.org/docs/7.4/static/sql-syntax.html#SQL-SYNTAX-IDENTIFIERS';
			throw new TDbException('dbcommon_invalid_identifier_name', $name, $ref);
		}
		return $name;
	}

	/**
	 * @param string table schema name
	 * @param string table name.
	 * @return boolean true if the table is a view.
	 */
	protected function getIsView($schemaName,$tableName)
	{
		$sql =
<<<EOD
		SELECT count(c.relname) FROM pg_catalog.pg_class c
		LEFT JOIN pg_catalog.pg_namespace n ON (n.oid = c.relnamespace)
		WHERE (n.nspname=:schema) AND (c.relkind = 'v'::"char") AND c.relname = :table
EOD;
		$this->getDbConnection()->setActive(true);
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':schema',$schemaName);
		$command->bindValue(':table', $tableName);
		return intval($command->queryScalar()) === 1;
	}

	/**
	 * @param TPgsqlTableInfo table information.
	 * @param array column information.
	 */
	protected function processColumn($tableInfo, $col)
	{
		$columnId = $col['attname']; //use column name as column Id

		$info['ColumnName'] = '"'.$columnId.'"'; //quote the column names!
		$info['ColumnId'] = $columnId;
		$info['ColumnIndex'] = $col['index'];
		if(!$col['attnotnull'])
			$info['AllowNull'] = true;
		if(in_array($columnId, $tableInfo->getPrimaryKeys()))
			$info['IsPrimaryKey'] = true;
		if($this->isForeignKeyColumn($columnId, $tableInfo))
			$info['IsForeignKey'] = true;

		if($col['atttypmod'] > 0)
			$info['ColumnSize'] =  $col['atttypmod'] - 4;
		if($col['atthasdef'])
			$info['DefaultValue'] = $col['adsrc'];
		if($col['attisserial'] || substr($col['adsrc'],0,8) === 'nextval(')
		{
			if(($sequence = $this->getSequenceName($tableInfo, $col['adsrc']))!==null)
			{
				$info['SequenceName'] = $sequence;
				unset($info['DefaultValue']);
			}
		}
		$matches = array();
		if(preg_match('/\((\d+)(?:,(\d+))?+\)/', $col['type'], $matches))
		{
			$info['DbType'] = preg_replace('/\(\d+(?:,\d+)?\)/','',$col['type']);
			if($this->isPrecisionType($info['DbType']))
			{
				$info['NumericPrecision'] = intval($matches[1]);
				if(count($matches) > 2)
					$info['NumericScale'] = intval($matches[2]);
			}
			else
				$info['ColumnSize'] = intval($matches[1]);
		}
		else
			$info['DbType'] = $col['type'];

		$tableInfo->Columns[$columnId] = new TPgsqlTableColumn($info);
	}

	/**
	 * @return string serial name if found, null otherwise.
	 */
	protected function getSequenceName($tableInfo,$src)
	{
		$matches = array();
		if(preg_match('/nextval\([^\']*\'([^\']+)\'[^\)]*\)/i',$src,$matches))
		{
			if(is_int(strpos($matches[1], '.')))
				return $matches[1];
			else
				return $tableInfo->getSchemaName().'.'.$matches[1];
		}
	}

	/**
	 * @return boolean true if column type if "numeric", "interval" or begins with "time".
	 */
	protected function isPrecisionType($type)
	{
		$type = strtolower(trim($type));
		return $type==='numeric' || $type==='interval' || strpos($type, 'time')===0;
	}

	/**
	 * Gets the primary and foreign key column details for the given table.
	 * @param string schema name
	 * @param string table name.
	 * @return array tuple ($primary, $foreign)
	 */
	protected function getConstraintKeys($schemaName, $tableName)
	{
		$sql =
<<<EOD
	SELECT conname, consrc, contype, indkey, indisclustered FROM (
			SELECT
					conname,
					CASE WHEN contype='f' THEN
							pg_catalog.pg_get_constraintdef(oid)
					ELSE
							'CHECK (' || consrc || ')'
					END AS consrc,
					contype,
					conrelid AS relid,
					NULL AS indkey,
					FALSE AS indisclustered
			FROM
					pg_catalog.pg_constraint
			WHERE
					contype IN ('f', 'c')
			UNION ALL
			SELECT
					pc.relname,
					NULL,
					CASE WHEN indisprimary THEN
							'p'
					ELSE
							'u'
					END,
					pi.indrelid,
					indkey,
					pi.indisclustered
			FROM
					pg_catalog.pg_class pc,
					pg_catalog.pg_index pi
			WHERE
					pc.oid=pi.indexrelid
					AND EXISTS (
							SELECT 1 FROM pg_catalog.pg_depend d JOIN pg_catalog.pg_constraint c
							ON (d.refclassid = c.tableoid AND d.refobjid = c.oid)
							WHERE d.classid = pc.tableoid AND d.objid = pc.oid AND d.deptype = 'i' AND c.contype IN ('u', 'p')
			)
	) AS sub
	WHERE relid = (SELECT oid FROM pg_catalog.pg_class WHERE relname=:table
					AND relnamespace = (SELECT oid FROM pg_catalog.pg_namespace
					WHERE nspname=:schema))
	ORDER BY
			1
EOD;
		$this->getDbConnection()->setActive(true);
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $tableName);
		$command->bindValue(':schema', $schemaName);
		$primary = array();
		$foreign = array();
		foreach($command->query() as $row)
		{
			switch($row['contype'])
			{
				case 'p':
					$primary = $this->getPrimaryKeys($tableName, $schemaName, $row['indkey']);
					break;
				case 'f':
					if(($fkey = $this->getForeignKeys($row['consrc']))!==null)
						$foreign[] = $fkey;
					break;
			}
		}
		return array($primary,$foreign);
	}

	/**
	 * Gets the primary key field names
	 * @param string pgsql primary key definition
	 * @return array primary key field names.
	 */
	protected function getPrimaryKeys($tableName, $schemaName, $columnIndex)
	{
		$index = join(', ', explode(' ', $columnIndex));
		$sql =
<<<EOD
    SELECT attnum, attname FROM pg_catalog.pg_attribute WHERE
		attrelid=(
			SELECT oid FROM pg_catalog.pg_class WHERE relname=:table AND relnamespace=(
				SELECT oid FROM pg_catalog.pg_namespace WHERE nspname=:schema
			)
		)
        AND attnum IN ({$index})
EOD;
		$command = $this->getDbConnection()->createCommand($sql);
		$command->bindValue(':table', $tableName);
		$command->bindValue(':schema', $schemaName);
//		$command->bindValue(':columnIndex', join(', ', explode(' ', $columnIndex)));
		$primary = array();
		foreach($command->query() as $row)
		{
            $primary[] = $row['attname'];
		}

		return $primary;
	}

	/**
	 * Gets foreign relationship constraint keys and table name
	 * @param string pgsql foreign key definition
	 * @return array foreign relationship table name and keys, null otherwise
	 */
	protected function getForeignKeys($src)
	{
		$matches = array();
		$brackets = '\(([^\)]+)\)';
		$find = "/FOREIGN\s+KEY\s+{$brackets}\s+REFERENCES\s+([^\(]+){$brackets}/i";
		if(preg_match($find, $src, $matches))
		{
			$keys = preg_split('/,\s+/', $matches[1]);
			$fkeys = array();
			foreach(preg_split('/,\s+/', $matches[3]) as $i => $fkey)
				$fkeys[$keys[$i]] = $fkey;
			return array('table' => str_replace('"','',$matches[2]), 'keys' => $fkeys);
		}
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

