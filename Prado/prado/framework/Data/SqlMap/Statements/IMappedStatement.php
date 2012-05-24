<?php
/**
 * IMappedStatement interface file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: IMappedStatement.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Statements
 */

/**
 * Interface for all mapping statements.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: IMappedStatement.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Statements
 * @since 3.1
 */
interface IMappedStatement
{
	/**
	 * @return string Name used to identify the MappedStatement amongst the others.
	 */
	public function getID();

	/**
	 * @return TSqlMapStatement The SQL statment used by this TMappedStatement.
	 */
	public function getStatement();

	/**
	 * @return TSqlMap The TSqlMap used by this TMappedStatement
	 */
	public function getManager();

	/**
	 * Executes the SQL and retuns all rows selected in a map that is keyed on
	 * the property named in the <tt>$keyProperty</tt> parameter.  The value at
	 * each key will be the value of the property specified  in the
	 * <tt>$valueProperty</tt> parameter.  If <tt>$valueProperty</tt> is
	 * <tt>null</tt>, the entire result object will be entered.
	 * @param IDbConnection database connection to execute the query
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param string The property of the result object to be used as the key.
	 * @param string The property of the result object to be used as the value (or null)
	 * @return TMap A map of object containing the rows keyed by <tt>$keyProperty</tt>.
	 */
	public function executeQueryForMap($connection, $parameter, $keyProperty, $valueProperty=null);


	/**
	 * Execute an update statement. Also used for delete statement. Return the
	 * number of row effected.
	 * @param IDbConnection database connection to execute the query
	 * @param mixed The object used to set the parameters in the SQL.
	 * @return integer The number of row effected.
	 */
	public function executeUpdate($connection, $parameter);


	/**
	 * Executes the SQL and retuns a subset of the rows selected.
	 * @param IDbConnection database connection to execute the query
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param TList A list to populate the result with.
	 * @param integer The number of rows to skip over.
	 * @param integer The maximum number of rows to return.
	 * @return TList A TList of result objects.
	 */
	public function executeQueryForList($connection, $parameter, $result=null, $skip=-1, $max=-1);


	/**
	 * Executes an SQL statement that returns a single row as an object
	 * of the type of the <tt>$result</tt> passed in as a parameter.
	 * @param IDbConnection database connection to execute the query
	 * @param mixed The object used to set the parameters in the SQL.
	 * @param object The result object.
	 * @return object result.
	 */
	public function executeQueryForObject($connection,$parameter, $result=null);
}

