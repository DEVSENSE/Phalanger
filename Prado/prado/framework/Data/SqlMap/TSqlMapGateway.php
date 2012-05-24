<?php
/**
 * TSqlMapGateway class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqlMapGateway.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.SqlMap
 */

Prado::using('System.Data.SqlMap.TSqlMapManager');

/**
 * DataMapper client, a fascade to provide access the rest of the DataMapper
 * framework. It provides three core functions:
 *
 *  # execute an update query (including insert and delete).
 *  # execute a select query for a single object
 *  # execute a select query for a list of objects
 *
 * This class should be instantiated from a TSqlMapManager instance.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TSqlMapGateway.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TSqlMapGateway extends TComponent
{
	/**
	 * @var TSqlMapManager manager
	 */
	private $_manager;

	public function __construct($manager)
	{
		$this->_manager=$manager;
	}

	/**
	 * @return TSqlMapManager sqlmap manager.
	 */
	public function getSqlMapManager()
	{
		return $this->_manager;
	}

	/**
	 * @return TDbConnection database connection.
	 */
	public function getDbConnection()
	{
		return $this->getSqlMapManager()->getDbConnection();
	}

	/**
	 * Executes a Sql SELECT statement that returns that returns data
	 * to populate a single object instance.
	 *
	 * The parameter object is generally used to supply the input
	 * data for the WHERE clause parameter(s) of the SELECT statement.
	 *
	 * @param string The name of the sql statement to execute.
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param mixed An object of the type to be returned.
	 * @return object A single result object populated with the result set data.
	 */
	public function queryForObject($statementName, $parameter=null, $result=null)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return $statement->executeQueryForObject($this->getDbConnection(), $parameter, $result);
	}

	/**
	 * Executes a Sql SELECT statement that returns data to populate a number
	 * of result objects.
	 *
	 * The parameter object is generally used to supply the input
	 * data for the WHERE clause parameter(s) of the SELECT statement.
	 *
	 * @param string The name of the sql statement to execute.
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param TList An Ilist object used to hold the objects,
	 * pass in null if want to return a list instead.
	 * @param int The number of rows to skip over.
	 * @param int The maximum number of rows to return.
	 * @return TList A List of result objects.
	 */
	public function queryForList($statementName, $parameter=null, $result=null, $skip=-1, $max=-1)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return $statement->executeQueryForList($this->getDbConnection(),$parameter, $result, $skip, $max);
	}

	/**
	 * Runs a query for list with a custom object that gets a chance to deal
	 * with each row as it is processed.
	 *
	 * Example: $sqlmap->queryWithRowDelegate('getAccounts', array($this, 'rowHandler'));
	 *
	 * @param string The name of the sql statement to execute.
	 * @param callback Row delegate handler, a valid callback required.
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param TList An Ilist object used to hold the objects,
	 * pass in null if want to return a list instead.
	 * @param int The number of rows to skip over.
	 * @param int The maximum number of rows to return.
	 * @return TList A List of result objects.
	 */
	public function queryWithRowDelegate($statementName, $delegate, $parameter=null, $result=null, $skip=-1, $max=-1)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return $statement->executeQueryForList($this->getDbConnection(), $parameter, $result, $skip, $max, $delegate);
	}

	/**
	 * Executes the SQL and retuns a subset of the results in a dynamic
	 * TPagedList that can be used to automatically scroll through results
	 * from a database table.
	 * @param string The name of the sql statement to execute.
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param integer The maximum number of objects to store in each page.
	 * @param integer The number of the page to initially load into the list.
	 * @return TPagedList A PaginatedList of beans containing the rows.
	 */
	public function queryForPagedList($statementName, $parameter=null, $pageSize=10, $page=0)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return new TSqlMapPagedList($statement, $parameter, $pageSize, null, $page);
	}

	/**
	 * Executes the SQL and retuns a subset of the results in a dynamic
	 * TPagedList that can be used to automatically scroll through results
	 * from a database table.
	 *
	 * Runs paged list query with row delegate
	 * Example: $sqlmap->queryForPagedListWithRowDelegate('getAccounts', array($this, 'rowHandler'));
	 *
	 * @param string The name of the sql statement to execute.
	 * @param callback Row delegate handler, a valid callback required.
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param integer The maximum number of objects to store in each page.
	 * @param integer The number of the page to initially load into the list.
	 * @return TPagedList A PaginatedList of beans containing the rows.
	 */
	public function queryForPagedListWithRowDelegate($statementName,$delegate, $parameter=null, $pageSize=10, $page=0)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return new TSqlMapPagedList($statement, $parameter, $pageSize, $delegate,$page);
	}


	/**
	 * Executes the SQL and retuns all rows selected in a map that is keyed on
	 * the property named  in the keyProperty parameter.  The value at each key
	 * will be the value of the property specified in the valueProperty
	 * parameter.  If valueProperty is null, the entire result object will be
	 * entered.
	 * @param string The name of the sql statement to execute.
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param string The property of the result object to be used as the key.
	 * @param string The property of the result object to be used as the value.
	 * @return TMap Array object containing the rows keyed by keyProperty.
	 */
	public function queryForMap($statementName, $parameter=null, $keyProperty=null, $valueProperty=null, $skip=-1, $max=-1)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return $statement->executeQueryForMap($this->getDbConnection(), $parameter, $keyProperty, $valueProperty, $skip, $max);
	}

	/**
	 * Runs a query with a custom object that gets a chance to deal
	 * with each row as it is processed.
	 *
	 * Example: $sqlmap->queryForMapWithRowDelegate('getAccounts', array($this, 'rowHandler'));
	 *
	 * @param string The name of the sql statement to execute.
	 * @param callback Row delegate handler, a valid callback required.
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param string The property of the result object to be used as the key.
	 * @param string The property of the result object to be used as the value.
	 * @return TMap Array object containing the rows keyed by keyProperty.
	 */
	public function queryForMapWithRowDelegate($statementName, $delegate, $parameter=null, $keyProperty=null, $valueProperty=null, $skip=-1, $max=-1)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return $statement->executeQueryForMap($this->getDbConnection(), $parameter, $keyProperty, $valueProperty, $skip, $max, $delegate);
	}

	/**
	 * Executes a Sql INSERT statement.
	 *
	 * Insert is a bit different from other update methods, as it provides
	 * facilities for returning the primary key of the newly inserted row
	 * (rather than the effected rows),
	 *
	 * The parameter object is generally used to supply the input data for the
	 * INSERT values.
	 *
	 * @param string The name of the statement to execute.
	 * @param string The parameter object.
	 * @return mixed The primary key of the newly inserted row.
	 * This might be automatically generated by the RDBMS,
	 * or selected from a sequence table or other source.
	 */
	public function insert($statementName, $parameter=null)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return $statement->executeInsert($this->getDbConnection(), $parameter);
	}

	/**
	 * Executes a Sql UPDATE statement.
	 *
	 * Update can also be used for any other update statement type, such as
	 * inserts and deletes.  Update returns the number of rows effected.
	 *
	 * The parameter object is generally used to supply the input data for the
	 * UPDATE values as well as the WHERE clause parameter(s).
	 *
	 * @param string The name of the statement to execute.
	 * @param mixed The parameter object.
	 * @return integer The number of rows effected.
	 */
	public function update($statementName, $parameter=null)
	{
		$statement = $this->getSqlMapManager()->getMappedStatement($statementName);
		return $statement->executeUpdate($this->getDbConnection(), $parameter);
	}

	/**
	 * Executes a Sql DELETE statement.  Delete returns the number of rows effected.
	 * @param string The name of the statement to execute.
	 * @param mixed The parameter object.
	 * @return integer The number of rows effected.
	 */
	public function delete($statementName, $parameter=null)
	{
		return $this->update($statementName, $parameter);
	}

	/**
	 * Flushes all cached objects that belong to this SqlMap
	 */
	public function flushCaches()
	{
		$this->getSqlMapManager()->flushCacheModels();
	}

	/**
	 * @param TSqlMapTypeHandler new type handler.
	 */
	public function registerTypeHandler($typeHandler)
	{
		$this->getSqlMapManager()->getTypeHandlers()->registerTypeHandler($typeHandler);
	}
}

