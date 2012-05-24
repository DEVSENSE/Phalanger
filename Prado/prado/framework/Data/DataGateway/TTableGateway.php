<?php
/**
 * TTableGateway class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Data.DataGateway
 */

/**
 * Loads the data gateway command builder and sql criteria.
 */
Prado::using('System.Data.DataGateway.TSqlCriteria');
Prado::using('System.Data.DataGateway.TDataGatewayCommand');

/**
 * TTableGateway class provides several find methods to get data from the database
 * and update, insert, and delete methods.
 *
 * Each method maps the input parameters into a SQL call and executes the SQL
 * against a database connection. The TTableGateway is stateless
 * (with respect to the data and data objects), as its role is to push data back and forth.
 *
 * Example usage:
 * <code>
 * //create a connection
 * $dsn = 'pgsql:host=localhost;dbname=test';
 * $conn = new TDbConnection($dsn, 'dbuser','dbpass');
 *
 * //create a table gateway for table/view named 'address'
 * $table = new TTableGateway('address', $conn);
 *
 * //insert a new row, returns last insert id (if applicable)
 * $id = $table->insert(array('name'=>'wei', 'phone'=>'111111'));
 *
 * $record1 = $table->findByPk($id); //find inserted record
 *
 * //finds all records, returns an iterator
 * $records = $table->findAll();
 * print_r($records->readAll());
 *
 * //update the row
 * $table->updateByPk($record1, $id);
 * </code>
 *
 * All methods that may return more than one row of data will return an
 * TDbDataReader iterator.
 *
 * The OnCreateCommand event is raised when a command is prepared and parameter
 * binding is completed. The parameter object is a TDataGatewayEventParameter of which the
 * {@link TDataGatewayEventParameter::getCommand Command} property can be
 * inspected to obtain the sql query to be executed.
 *
 * The OnExecuteCommand	event is raised when a command is executed and the result
 * from the database was returned. The parameter object is a
 * TDataGatewayResultEventParameter of which the
 * {@link TDataGatewayEventParameter::getResult Result} property contains
 * the data return from the database. The data returned can be changed
 * by setting the {@link TDataGatewayEventParameter::setResult Result} property.
 *
 * <code>
 * $table->OnCreateCommand[] = 'log_it'; //any valid PHP callback statement
 * $table->OnExecuteCommand[] = array($obj, 'method_name'); // calls 'method_name' on $obj
 *
 * function log_it($sender, $param)
 * {
 *     var_dump($param); //TDataGatewayEventParameter object.
 * }
 * </code>
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id$
 * @package System.Data.DataGateway
 * @since 3.1
 */
class TTableGateway extends TComponent
{
	private $_command;
	private $_connection;

	/**
	 * Creates a new generic table gateway for a given table or view name
	 * and a database connection.
	 * @param string|TDbTableInfo table or view name or table information.
	 * @param TDbConnection database connection.
	 */
	public function __construct($table,$connection)
	{
		$this->_connection=$connection;
		if(is_string($table))
			$this->setTableName($table);
		else if($table instanceof TDbTableInfo)
			$this->setTableInfo($table);
		else
			throw new TDbException('dbtablegateway_invalid_table_info');
	}

	/**
	 * @param TDbTableInfo table or view information.
	 */
	protected function setTableInfo($tableInfo)
	{
		$builder = $tableInfo->createCommandBuilder($this->getDbConnection());
		$this->initCommandBuilder($builder);
	}

	/**
	 * Sets up the command builder for the given table.
	 * @param string table or view name.
	 */
	protected function setTableName($tableName)
	{
		Prado::using('System.Data.Common.TDbMetaData');
		$meta = TDbMetaData::getInstance($this->getDbConnection());
		$this->initCommandBuilder($meta->createCommandBuilder($tableName));
	}

	public function getTableInfo()
	{
		return $this->getCommand()->getTableInfo();
	}

	public function getTableName()
	{
		return $this->getTableInfo()->getTableName();
	}

	/**
	 * @param TDbCommandBuilder database specific command builder.
	 */
	protected function initCommandBuilder($builder)
	{
		$this->_command = new TDataGatewayCommand($builder);
		$this->_command->OnCreateCommand[] = array($this, 'onCreateCommand');
		$this->_command->OnExecuteCommand[] = array($this, 'onExecuteCommand');
	}

	/**
	 * Raised when a command is prepared and parameter binding is completed.
	 * The parameter object is TDataGatewayEventParameter of which the
	 * {@link TDataGatewayEventParameter::getCommand Command} property can be
	 * inspected to obtain the sql query to be executed.
	 * @param TDataGatewayCommand originator $sender
	 * @param TDataGatewayEventParameter
	 */
	public function onCreateCommand($sender, $param)
	{
		$this->raiseEvent('OnCreateCommand', $this, $param);
	}

	/**
	 * Raised when a command is executed and the result from the database was returned.
	 * The parameter object is TDataGatewayResultEventParameter of which the
	 * {@link TDataGatewayEventParameter::getResult Result} property contains
	 * the data return from the database. The data returned can be changed
	 * by setting the {@link TDataGatewayEventParameter::setResult Result} property.
	 * @param TDataGatewayCommand originator $sender
	 * @param TDataGatewayResultEventParameter
	 */
	public function onExecuteCommand($sender, $param)
	{
		$this->raiseEvent('OnExecuteCommand', $this, $param);
	}

	/**
	 * @return TDataGatewayCommand command builder and executor.
	 */
	protected function getCommand()
	{
		return $this->_command;
	}

	/**
	 * @return TDbConnection database connection.
	 */
	public function getDbConnection()
	{
		return $this->_connection;
	}

	/**
	 * Execute arbituary sql command with binding parameters.
	 * @param string SQL query string.
	 * @param array binding parameters, positional or named.
	 * @return array query results.
	 */
	public function findBySql($sql, $parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getCriteria($sql,$parameters, $args);
		return $this->getCommand()->findBySql($criteria);
	}

	/**
	 * Execute arbituary sql command with binding parameters.
	 * @param string SQL query string.
	 * @param array binding parameters, positional or named.
	 * @return TDbDataReader query results.
	 */
	public function findAllBySql($sql, $parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getCriteria($sql,$parameters, $args);
		return $this->getCommand()->findAllBySql($criteria);
	}

	/**
	 * Find one single record that matches the criteria.
	 *
	 * Usage:
	 * <code>
	 * $table->find('username = :name AND password = :pass',
	 * 					array(':name'=>$name, ':pass'=>$pass));
	 * $table->find('username = ? AND password = ?', array($name, $pass));
	 * $table->find('username = ? AND password = ?', $name, $pass);
	 * //$criteria is of TSqlCriteria
	 * $table->find($criteria); //the 2nd parameter for find() is ignored.
	 * </code>
	 *
	 * @param string|TSqlCriteria SQL condition or criteria object.
	 * @param mixed parameter values.
	 * @return array matching record object.
	 */
	public function find($criteria, $parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getCriteria($criteria,$parameters, $args);
		return $this->getCommand()->find($criteria);
	}

	/**
	 * Accepts same parameters as find(), but returns TDbDataReader instead.
	 * @param string|TSqlCriteria SQL condition or criteria object.
	 * @param mixed parameter values.
	 * @return TDbDataReader matching records.
	 */
	public function findAll($criteria=null, $parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		if($criteria!==null)
			$criteria = $this->getCriteria($criteria,$parameters, $args);
		return $this->getCommand()->findAll($criteria);
	}

	/**
	 * Find one record using only the primary key or composite primary keys. Usage:
	 *
	 * <code>
	 * $table->findByPk($primaryKey);
	 * $table->findByPk($key1, $key2, ...);
	 * $table->findByPk(array($key1,$key2,...));
	 * </code>
	 *
	 * @param mixed primary keys
	 * @return array matching record.
	 */
	public function findByPk($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		return $this->getCommand()->findByPk($keys);
	}

	/**
	 * Similar to findByPk(), but returns TDbDataReader instead.
	 *
	 * For scalar primary keys:
	 * <code>
	 * $table->findAllByPk($key1, $key2, ...);
	 * $table->findAllByPk(array($key1, $key2, ...));
	 * </code>
	 *
	 * For composite keys:
	 * <code>
	 * $table->findAllByPk(array($key1, $key2), array($key3, $key4), ...);
	 * $table->findAllByPk(array(array($key1, $key2), array($key3, $key4), ...));
	 * </code>
	 * @param mixed primary keys
	 * @return TDbDataReader data reader.
	 */
	public function findAllByPks($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		return $this->getCommand()->findAllByPk($keys);
	}

	/**
	 * Delete records from the table with condition given by $where and
	 * binding values specified by $parameter argument.
	 * This method uses additional arguments as $parameters. E.g.
	 * <code>
	 * $table->delete('age > ? AND location = ?', $age, $location);
	 * </code>
	 * @param string delete condition.
	 * @param array condition parameters.
	 * @return integer number of records deleted.
	 */
	public function deleteAll($criteria, $parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getCriteria($criteria,$parameters, $args);
		return $this->getCommand()->delete($criteria);
	}

	/**
	 * Delete records by primary key. Usage:
	 *
	 * <code>
	 * $table->deleteByPk($primaryKey); //delete 1 record
	 * $table->deleteByPk($key1,$key2,...); //delete multiple records
	 * $table->deleteByPk(array($key1,$key2,...)); //delete multiple records
	 * </code>
	 *
	 * For composite primary keys (determined from the table definitions):
	 * <code>
	 * $table->deleteByPk(array($key1,$key2)); //delete 1 record
	 *
	 * //delete multiple records
	 * $table->deleteByPk(array($key1,$key2), array($key3,$key4),...);
	 *
	 * //delete multiple records
	 * $table->deleteByPk(array( array($key1,$key2), array($key3,$key4), .. ));
	 * </code>
	 *
	 * @param mixed primary key values.
	 * @return int number of records deleted.
	 */
	public function deleteByPk($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		return $this->getCommand()->deleteByPk($keys);
	}

	/**
	 * Alias for deleteByPk()
	 */
	public function deleteAllByPks($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		return $this->deleteByPk($keys);
	}

	/**
	 * Find the number of records.
	 * @param string|TSqlCriteria SQL condition or criteria object.
	 * @param mixed parameter values.
	 * @return int number of records.
	 */
	public function count($criteria=null,$parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		if($criteria!==null)
			$criteria = $this->getCriteria($criteria,$parameters, $args);
		return $this->getCommand()->count($criteria);
	}

	/**
	 * Updates the table with new name-value pair $data. Each array key must
	 * correspond to a column name in the table. The update condition is
	 * specified by the $where argument and additional binding values can be
	 * specified using the $parameter argument.
	 * This method uses additional arguments as $parameters. E.g.
	 * <code>
	 * $gateway->update($data, 'age > ? AND location = ?', $age, $location);
	 * </code>
	 * @param array new record data.
	 * @param string update condition
	 * @param array additional binding name-value pairs.
	 * @return integer number of records updated.
	 */
	public function update($data, $criteria, $parameters=array())
	{
		$args = func_num_args() > 2 ? array_slice(func_get_args(),2) : null;
		$criteria = $this->getCriteria($criteria,$parameters, $args);
		return $this->getCommand()->update($data, $criteria);
	}

	/**
	 * Inserts a new record into the table. Each array key must
	 * correspond to a column name in the table unless a null value is permitted.
	 * @param array new record data.
	 * @return mixed last insert id if one column contains a serial or sequence,
	 * otherwise true if command executes successfully and affected 1 or more rows.
	 */
	public function insert($data)
	{
		return $this->getCommand()->insert($data);
	}

	/**
	 * @return mixed last insert id, null if none is found.
	 */
	public function getLastInsertId()
	{
		return $this->getCommand()->getLastInsertId();
	}

	/**
	 * Create a new TSqlCriteria object from a string $criteria. The $args
	 * are additional parameters and are used in place of the $parameters
	 * if $parameters is not an array and $args is an arrary.
	 * @param string|TSqlCriteria sql criteria
	 * @param mixed parameters passed by the user.
	 * @param array additional parameters obtained from function_get_args().
	 * @return TSqlCriteria criteria object.
	 */
	protected function getCriteria($criteria, $parameters, $args)
	{
		if(is_string($criteria))
		{
			$useArgs = !is_array($parameters) && is_array($args);
			return new TSqlCriteria($criteria,$useArgs ? $args : $parameters);
		}
		else if($criteria instanceof TSqlCriteria)
			return $criteria;
		else
			throw new TDbException('dbtablegateway_invalid_criteria');
	}

	/**
	 * Dynamic find method using parts of method name as search criteria.
	 * Method name starting with "findBy" only returns 1 record.
	 * Method name starting with "findAllBy" returns 0 or more records.
	 * Method name starting with "deleteBy" deletes records by the trail criteria.
	 * The condition is taken as part of the method name after "findBy", "findAllBy"
	 * or "deleteBy".
	 *
	 * The following are equivalent:
	 * <code>
	 * $table->findByName($name)
	 * $table->find('Name = ?', $name);
	 * </code>
	 * <code>
	 * $table->findByUsernameAndPassword($name,$pass); // OR may be used
	 * $table->findBy_Username_And_Password($name,$pass); // _OR_ may be used
	 * $table->find('Username = ? AND Password = ?', $name, $pass);
	 * </code>
	 * <code>
	 * $table->findAllByAge($age);
	 * $table->findAll('Age = ?', $age);
	 * </code>
	 * <code>
	 * $table->deleteAll('Name = ?', $name);
	 * $table->deleteByName($name);
	 * </code>
	 * @return mixed single record if method name starts with "findBy", 0 or more records
	 * if method name starts with "findAllBy"
	 */
	public function __call($method,$args)
	{
		$delete =false;
		if($findOne = substr(strtolower($method),0,6)==='findby')
			$condition = $method[6]==='_' ? substr($method,7) : substr($method,6);
		else if(substr(strtolower($method),0,9)==='findallby')
			$condition = $method[9]==='_' ? substr($method,10) : substr($method,9);
		else if($delete = substr(strtolower($method),0,8)==='deleteby')
			$condition = $method[8]==='_' ? substr($method,9) : substr($method,8);
		else if($delete = substr(strtolower($method),0,11)==='deleteallby')
			$condition = $method[11]==='_' ? substr($method,12) : substr($method,11);
		else
			return null;

		$criteria = $this->getCommand()->createCriteriaFromString($method, $condition, $args);
		if($delete)
			return $this->deleteAll($criteria);
		else
			return $findOne ? $this->find($criteria) : $this->findAll($criteria);
	}
}

