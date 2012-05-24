<?php
/**
 * TDbCommandBuilder class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbCommandBuilder.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 */

/**
 * TDbCommandBuilder provides basic methods to create query commands for tables
 * giving by {@link setTableInfo TableInfo} the property.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDbCommandBuilder.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 * @since 3.1
 */
class TDbCommandBuilder extends TComponent
{
	private $_connection;
	private $_tableInfo;

	/**
	 * @param TDbConnection database connection.
	 * @param TDbTableInfo table information.
	 */
	public function __construct($connection=null, $tableInfo=null)
	{
		$this->setDbConnection($connection);
		$this->setTableInfo($tableInfo);
	}

	/**
	 * @return TDbConnection database connection.
	 */
	public function getDbConnection()
	{
		return $this->_connection;
	}

	/**
	 * @param TDbConnection database connection.
	 */
	public function setDbConnection($value)
	{
		$this->_connection=$value;
	}

	/**
	 * @param TDbTableInfo table information.
	 */
	public function setTableInfo($value)
	{
		$this->_tableInfo=$value;
	}

	/**
	 * @param TDbTableInfo table information.
	 */
	public function getTableInfo()
	{
		return $this->_tableInfo;
	}

	/**
	 * Iterate through all the columns and returns the last insert id of the
	 * first column that has a sequence or serial.
	 * @return mixed last insert id, null if none is found.
	 */
	public function getLastInsertID()
	{
		foreach($this->getTableInfo()->getColumns() as $column)
		{
			if($column->hasSequence())
				return $this->getDbConnection()->getLastInsertID($column->getSequenceName());
		}
	}

	/**
	 * Alters the sql to apply $limit and $offset. Default implementation is applicable
	 * for PostgreSQL, MySQL and SQLite.
	 * @param string SQL query string.
	 * @param integer maximum number of rows, -1 to ignore limit.
	 * @param integer row offset, -1 to ignore offset.
	 * @return string SQL with limit and offset.
	 */
	public function applyLimitOffset($sql, $limit=-1, $offset=-1)
	{
		$limit = $limit!==null ? (int)$limit : -1;
		$offset = $offset!==null ? (int)$offset : -1;
		$limitStr = $limit >= 0 ? ' LIMIT '.$limit : '';
		$offsetStr = $offset >= 0 ? ' OFFSET '.$offset : '';
		return $sql.$limitStr.$offsetStr;
	}

	/**
	 * @param string SQL string without existing ordering.
	 * @param array pairs of column names as key and direction as value.
	 * @return string modified SQL applied with ORDER BY.
	 */
	public function applyOrdering($sql, $ordering)
	{
		$orders=array();
		foreach($ordering as $name => $direction)
		{
			$direction = strtolower($direction) == 'desc' ? 'DESC' : 'ASC';
			if(false !== strpos($name, '(') && false !== strpos($name, ')')) {
				// key is a function (bad practice, but we need to handle it)
				$key = $name;
			} else {
				// key is a column
				$key = $this->getTableInfo()->getColumn($name)->getColumnName();
			}
			$orders[] = $key.' '.$direction;
		}
		if(count($orders) > 0)
			$sql .= ' ORDER BY '.implode(', ', $orders);
		return $sql;
	}

	/**
	 * Computes the SQL condition for search a set of column using regular expression
	 * (or LIKE, depending on database implementation) to match a string of
	 * keywords (default matches all keywords).
	 * @param array list of column id for potential search condition.
	 * @param string string of keywords
	 * @return string SQL search condition matching on a set of columns.
	 */
	public function getSearchExpression($fields, $keywords)
	{
		if(strlen(trim($keywords)) == 0) return '';
		$words = preg_split('/\s/u', $keywords);
		$conditions = array();
		foreach($fields as $field)
		{
			$column = $this->getTableInfo()->getColumn($field)->getColumnName();
			$conditions[] = $this->getSearchCondition($column, $words);
		}
		return '('.implode(' OR ', $conditions).')';
	}

	/**
	 * @param string column name.
	 * @param array keywords
	 * @return string search condition for all words in one column.
	 */
	protected function getSearchCondition($column, $words)
	{
		$conditions=array();
		foreach($words as $word)
			$conditions[] = $column.' LIKE '.$this->getDbConnection()->quoteString('%'.$word.'%');
		return '('.implode(' AND ', $conditions).')';
	}

	/**
	 *
	 * Different behavior depends on type of passed data
	 * string
	 * 	usage without modification
	 *
	 * null
	 * 	will be expanded to full list of quoted table column names (quoting depends on database)
	 *
	 * array
	 * - Column names will be quoted if used as key or value of array
	 * 	<code>
	 * 	array('col1', 'col2', 'col2')
	 * 	// SELECT `col1`, `col2`, `col3` FROM...
	 * 	</code>
	 *
	 * - Column aliasing
	 * <code>
	 * array('mycol1' => 'col1', 'mycol2' => 'COUNT(*)')
	 * // SELECT `col1` AS mycol1, COUNT(*) AS mycol2 FROM...
	 * </code>
	 *
	 * - NULL and scalar values (strings will be quoted depending on database)
	 * <code>
	 * array('col1' => 'my custom string', 'col2' => 1.0, 'col3' => 'NULL')
	 * // SELECT "my custom string" AS `col1`, 1.0 AS `col2`, NULL AS `col3` FROM...
	 * </code>
	 *
	 * - If the *-wildcard char is used as key or value, add the full list of quoted table column names
	 * <code>
	 * array('col1' => 'NULL', '*')
	 * // SELECT `col1`, `col2`, `col3`, NULL AS `col1` FROM...
	 * </code>
	 * @param mixed $value
	 * @return array of generated fields - use implode(', ', $selectfieldlist) to collapse field list for usage
	 * @since 3.1.7
	 * @todo add support for table aliasing
	 * @todo add support for quoting of column aliasing
	 */
	public function getSelectFieldList($data='*') {
		if(is_scalar($data)) {
			$tmp = explode(',', $data);
			$result = array();
			foreach($tmp as $v)
				$result[] = trim($v);
			return $result;
		}

		$bHasWildcard = false;
		$result = array();
		if(is_array($data) || $data instanceof Traversable) {
			$columns = $this->getTableInfo()->getColumns();
			foreach($data as $key=>$value) {
				if($key==='*' || $value==='*') {
					$bHasWildcard = true;
					continue;
				}

				if(strToUpper($key)==='NULL') {
					$result[] = 'NULL';
					continue;
				}

				if(strpos($key, '(')!==false && strpos($key, ')')!==false) {
					$result[] = $key;
					continue;
				}

				if(stripos($key, 'AS')!==false) {
					$result[] = $key;
					continue;
				}

				if(stripos($value, 'AS')!==false) {
					$result[] = $value;
					continue;
				}

				$v = isset($columns[$value]);
				$k = isset($columns[$key]);
				if(is_integer($key) && $v) {
					$key = $value;
					$k = $v;
				}

				if(strToUpper($value)==='NULL') {
					if($k)
						$result[] = 'NULL AS ' . $columns[$key]->getColumnName();
					else
						$result[] = 'NULL' . (is_string($key) ? (' AS ' . (string)$key) : '');
					continue;
				}

				if(strpos($value, '(')!==false && strpos($value, ')')!==false) {
					if($k)
						$result[] = $value . ' AS ' . $columns[$key]->getColumnName();
					else
						$result[] = $value . (is_string($key) ? (' AS ' . (string)$key) : '');
					continue;
				}

				if($v && $key==$value) {
					$result[] = $columns[$value]->getColumnName();
					continue;
				}

				if($k && $value==null) {
					$result[] = $columns[$key]->getColumnName();
					continue;
				}

				if(is_string($key) && $v) {
					$result[] = $columns[$value]->getColumnName() . ' AS ' . $key;
					continue;
				}

				if(is_numeric($value) && $k) {
					$result[] = $value . ' AS ' . $columns[$key]->getColumnName();
					continue;
				}

				if(is_string($value) && $k) {
					$result[] = $this->getDbConnection()->quoteString($value) . ' AS ' . $columns[$key]->getColumnName();
					continue;
				}

				if(!$v && !$k && is_integer($key)) {
					$result[] = is_numeric($value) ? $value : $this->getDbConnection()->quoteString((string)$value);
					continue;
				}

				$result[] = (is_numeric($value) ? $value : $this->getDbConnection()->quoteString((string)$value)) . ' AS ' . $key;
			}
		}

		if($data===null || count($result) == 0 || $bHasWildcard)
			$result = $result = array_merge($this->getTableInfo()->getColumnNames(), $result);

		return $result;
	}

	/**
	 * Appends the $where condition to the string "SELECT * FROM tableName WHERE ".
	 * The tableName is obtained from the {@link setTableInfo TableInfo} property.
	 * @param string query condition
	 * @param array condition parameters.
	 * @return TDbCommand query command.
	 */
	public function createFindCommand($where='1=1', $parameters=array(), $ordering=array(), $limit=-1, $offset=-1, $select='*')
	{
		$table = $this->getTableInfo()->getTableFullName();
		$fields = implode(', ', $this -> getSelectFieldList($select));
		$sql = "SELECT {$fields} FROM {$table}";
		if(!empty($where))
			$sql .= " WHERE {$where}";
		return $this->applyCriterias($sql, $parameters, $ordering, $limit, $offset);
	}

	public function applyCriterias($sql, $parameters=array(),$ordering=array(), $limit=-1, $offset=-1)
	{
		if(count($ordering) > 0)
			$sql = $this->applyOrdering($sql, $ordering);
		if($limit>=0 || $offset>=0)
			$sql = $this->applyLimitOffset($sql, $limit, $offset);
		$command = $this->createCommand($sql);
		$this->bindArrayValues($command, $parameters);
		return $command;
	}

	/**
	 * Creates a count(*) command for the table described in {@link setTableInfo TableInfo}.
	 * @param string count condition.
	 * @param array binding parameters.
	 * @return TDbCommand count command.
	 */
	public function createCountCommand($where='1=1', $parameters=array(),$ordering=array(), $limit=-1, $offset=-1)
	{
		return $this->createFindCommand($where, $parameters, $ordering, $limit, $offset, 'COUNT(*)');
	}

	/**
	 * Creates a delete command for the table described in {@link setTableInfo TableInfo}.
	 * The conditions for delete is given by the $where argument and the parameters
	 * for the condition is given by $parameters.
	 * @param string delete condition.
	 * @param array delete parameters.
	 * @return TDbCommand delete command.
	 */
	public function createDeleteCommand($where,$parameters=array())
	{
		$table = $this->getTableInfo()->getTableFullName();
		if (!empty($where))
			$where = ' WHERE '.$where;
		$command = $this->createCommand("DELETE FROM {$table}".$where);
		$this->bindArrayValues($command, $parameters);
		return $command;
	}

	/**
	 * Creates an insert command for the table described in {@link setTableInfo TableInfo} for the given data.
	 * Each array key in the $data array must correspond to the column name of the table
	 * (if a column allows to be null, it may be omitted) to be inserted with
	 * the corresponding array value.
	 * @param array name-value pairs of new data to be inserted.
	 * @return TDbCommand insert command
	 */
	public function createInsertCommand($data)
	{
		$table = $this->getTableInfo()->getTableFullName();
		list($fields, $bindings) = $this->getInsertFieldBindings($data);
		$command = $this->createCommand("INSERT INTO {$table}({$fields}) VALUES ($bindings)");
		$this->bindColumnValues($command, $data);
		return $command;
	}

	/**
	 * Creates an update command for the table described in {@link setTableInfo TableInfo} for the given data.
	 * Each array key in the $data array must correspond to the column name to be updated with the corresponding array value.
	 * @param array name-value pairs of data to be updated.
	 * @param string update condition.
	 * @param array update parameters.
	 * @return TDbCommand update command.
	 */
	public function createUpdateCommand($data, $where, $parameters=array())
	{
		$table = $this->getTableInfo()->getTableFullName();
		if($this->hasIntegerKey($parameters))
			$fields = implode(', ', $this->getColumnBindings($data, true));
		else
			$fields = implode(', ', $this->getColumnBindings($data));

		if (!empty($where))
			$where = ' WHERE '.$where;
		$command = $this->createCommand("UPDATE {$table} SET {$fields}".$where);
		$this->bindArrayValues($command, array_merge($data, $parameters));
		return $command;
	}

	/**
	 * Returns a list of insert field name and a list of binding names.
	 * @param object array or object to be inserted.
	 * @return array tuple ($fields, $bindings)
	 */
	protected function getInsertFieldBindings($values)
	{
		$fields = array(); $bindings=array();
		foreach(array_keys($values) as $name)
		{
			$fields[] = $this->getTableInfo()->getColumn($name)->getColumnName();
			$bindings[] = ':'.$name;
		}
		return array(implode(', ',$fields), implode(', ', $bindings));
	}

	/**
	 * Create a name-value or position-value if $position=true binding strings.
	 * @param array data for binding.
	 * @param boolean true to bind as position values.
	 * @return string update column names with corresponding binding substrings.
	 */
	protected function getColumnBindings($values, $position=false)
	{
		$bindings=array();
		foreach(array_keys($values) as $name)
		{
			$column = $this->getTableInfo()->getColumn($name)->getColumnName();
			$bindings[] = $position ? $column.' = ?' : $column.' = :'.$name;
		}
		return $bindings;
	}

	/**
	 * @param string SQL query string.
	 * @return TDbCommand corresponding database command.
	 */
	public function createCommand($sql)
	{
		$this->getDbConnection()->setActive(true);
		return $this->getDbConnection()->createCommand($sql);
	}

	/**
	 * Bind the name-value pairs of $values where the array keys correspond to column names.
	 * @param TDbCommand database command.
	 * @param array name-value pairs.
	 */
	public function bindColumnValues($command, $values)
	{
		foreach($values as $name=>$value)
		{
			$column = $this->getTableInfo()->getColumn($name);
			if($value === null && $column->getAllowNull())
				$command->bindValue(':'.$name, null, PDO::PARAM_NULL);
			else
				$command->bindValue(':'.$name, $value, $column->getPdoType());
		}
	}

	/**
	 * @param TDbCommand database command
	 * @param array values for binding.
	 */
	public function bindArrayValues($command, $values)
	{
		if($this->hasIntegerKey($values))
		{
			$values = array_values($values);
			for($i = 0, $max=count($values); $i<$max; $i++)
				$command->bindValue($i+1, $values[$i], $this->getPdoType($values[$i]));
		}
		else
		{
			foreach($values as $name=>$value)
			{
				$prop = $name[0]===':' ? $name : ':'.$name;
				$command->bindValue($prop, $value, $this->getPdoType($value));
			}
		}
	}

	/**
	 * @param mixed PHP value
	 * @return integer PDO parameter types.
	 */
	public static function getPdoType($value)
	{
		switch(gettype($value))
		{
			case 'boolean': return PDO::PARAM_BOOL;
			case 'integer': return PDO::PARAM_INT;
			case 'string' : return PDO::PARAM_STR;
			case 'NULL'   : return PDO::PARAM_NULL;
		}
	}

	/**
	 * @param array
	 * @return boolean true if any array key is an integer.
	 */
	protected function hasIntegerKey($array)
	{
		foreach($array as $k=>$v)
		{
			if(gettype($k)==='integer')
				return true;
		}
		return false;
	}
}
