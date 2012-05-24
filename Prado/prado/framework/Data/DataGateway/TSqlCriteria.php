<?php
/**
 * TDbSqlCriteria class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbSqlCriteria.php 1835 2007-04-03 01:38:15Z wei $
 * @package System.Data.DataGateway
 */

/**
 * Search criteria for TDbDataGateway.
 *
 * Criteria object for data gateway finder methods. Usage:
 * <code>
 * $criteria = new TSqlCriteria();
 * $criteria->Parameters[':name'] = 'admin';
 * $criteria->Parameters[':pass'] = 'prado';
 * $criteria->OrdersBy['level'] = 'desc';
 * $criteria->OrdersBy['name'] = 'asc';
 * $criteria->Limit = 10;
 * $criteria->Offset = 20;
 * </code>
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDbSqlCriteria.php 1835 2007-04-03 01:38:15Z wei $
 * @package System.Data.DataGateway
 * @since 3.1
 */
class TSqlCriteria extends TComponent
{
	/**
	 * @var mixed
	 * @since 3.1.7
	 */
	private $_select='*';
	private $_condition;
	private $_parameters;
	private $_ordersBy;
	private $_limit;
	private $_offset;

	/**
	 * Creates a new criteria with given condition;
	 * @param string sql string after the WHERE stanza
	 * @param mixed named or indexed parameters, accepts as multiple arguments.
	 */
	public function __construct($condition=null, $parameters=array())
	{
		if(!is_array($parameters) && func_num_args() > 1)
			$parameters = array_slice(func_get_args(),1);
		$this->_parameters=new TAttributeCollection;
		$this->_parameters->setCaseSensitive(true);
		$this->_parameters->copyFrom((array)$parameters);
		$this->_ordersBy=new TAttributeCollection;
		$this->_ordersBy->setCaseSensitive(true);

		$this->setCondition($condition);
	}

	/**
	 * Gets the field list to be placed after the SELECT in the SQL. Default to '*'
	 * @return mixed
	 * @since 3.1.7
	 */
	public function getSelect()
	{
		return $this->_select;
	}

	/**
	 * Sets the field list to be placed after the SELECT in the SQL.
	 *
	 * Different behavior depends on type of assigned value
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
	 *
	 * @param mixed
	 * @since 3.1.7
	 * @see TDbCommandBuilder::getSelectFieldList()
	 */
	public function setSelect($value)
	{
		$this->_select = $value;
	}

	/**
	 * @return string search conditions.
	 */
	public function getCondition()
	{
		return $this->_condition;
	}

	/**
	 * Sets the search conditions to be placed after the WHERE clause in the SQL.
	 * @param string search conditions.
	 */
	public function setCondition($value)
	{
		if(empty($value)) {
			return;
		}

		// supporting the following SELECT-syntax:
		// [ORDER BY {col_name | expr | position}
		//      [ASC | DESC], ...]
		//    [LIMIT {[offset,] row_count | row_count OFFSET offset}]
		// See: http://dev.mysql.com/doc/refman/5.0/en/select.html

		if(preg_match('/ORDER\s+BY\s+(.*?)(?=LIMIT)|ORDER\s+BY\s+(.*?)$/i', $value, $matches) > 0) {
			// condition contains ORDER BY
			$value = str_replace($matches[0], '', $value);
			if(strlen($matches[1]) > 0) {
				$this->setOrdersBy($matches[1]);
			} else if(strlen($matches[2]) > 0) {
				$this->setOrdersBy($matches[2]);
			}
		}

		if(preg_match('/LIMIT\s+([\d\s,]+)/i', $value, $matches) > 0) {
			// condition contains limit
			$value = str_replace($matches[0], '', $value); // remove limit from query
			if(strpos($matches[1], ',')) { // both offset and limit given
				list($offset, $limit) = explode(',', $matches[1]);
				$this->_limit = (int)$limit;
				$this->_offset = (int)$offset;
			} else { // only limit given
				$this->_limit = (int)$matches[1];
			}
		}

		if(preg_match('/OFFSET\s+(\d+)/i', $value, $matches) > 0) {
			// condition contains offset
			$value = str_replace($matches[0], '', $value); // remove offset from query
			$this->_offset = (int)$matches[1]; // set offset in criteria
		}

		$this->_condition = trim($value);
	}

	/**
	 * @return TAttributeCollection list of named parameters and values.
	 */
	public function getParameters()
	{
		return $this->_parameters;
	}

	/**
	 * @param ArrayAccess named parameters.
	 */
	public function setParameters($value)
	{
		if(!(is_array($value) || $value instanceof ArrayAccess))
			throw new TException('value must be array or ArrayAccess');
		$this->_parameters->copyFrom($value);
	}

	/**
	 * @return boolean true if the parameter index are string base, false otherwise.
	 */
	public function getIsNamedParameters()
	{
		foreach($this->getParameters() as $k=>$v)
			return is_string($k);
	}

	/**
	 * @return TAttributeCollection ordering clause.
	 */
	public function getOrdersBy()
	{
		return $this->_ordersBy;
	}

	/**
	 * @param mixed ordering clause.
	 */
	public function setOrdersBy($value)
	{
		if(is_array($value) || $value instanceof Traversable)
			$this->_ordersBy->copyFrom($value);
		else
		{
			$value=trim(preg_replace('/\s+/',' ',(string)$value));
			$orderBys=array();
			foreach(explode(',',$value) as $orderBy)
			{
				$vs=explode(' ',trim($orderBy));
				$orderBys[$vs[0]]=isset($vs[1])?$vs[1]:'asc';
			}
			$this->_ordersBy->copyFrom($orderBys);
		}
	}

	/**
	 * @return int maximum number of records to return.
	 */
	public function getLimit()
	{
		return $this->_limit;
	}

	/**
	 * @param int maximum number of records to return.
	 */
	public function setLimit($value)
	{
		$this->_limit=$value;
	}

	/**
	 * @return int record offset.
	 */
	public function getOffset()
	{
		return $this->_offset;
	}

	/**
	 * @param int record offset.
	 */
	public function setOffset($value)
	{
		$this->_offset=$value;
	}

	/**
	 * @return string string representation of the parameters. Useful for debugging.
	 */
	public function __toString()
	{
		$str = '';
		if(strlen((string)$this->getCondition()) > 0)
			$str .= '"'.(string)$this->getCondition().'"';
		$params = array();
		foreach($this->getParameters() as $k=>$v)
			$params[] = "{$k} => ${v}";
		if(count($params) > 0)
			$str .= ', "'.implode(', ',$params).'"';
		$orders = array();
		foreach($this->getOrdersBy() as $k=>$v)
			$orders[] = "{$k} => ${v}";
		if(count($orders) > 0)
			$str .= ', "'.implode(', ',$orders).'"';
		if($this->_limit !==null)
			$str .= ', '.$this->_limit;
		if($this->_offset !== null)
			$str .= ', '.$this->_offset;
		return $str;
	}
}
?>