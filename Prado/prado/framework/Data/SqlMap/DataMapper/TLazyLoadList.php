<?php
/**
 * TLazyLoadList, TObjectProxy classes file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TLazyLoadList.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 */

/**
 * TLazyLoadList executes mapped statements when the proxy collection is first accessed.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TLazyLoadList.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TLazyLoadList
{
	private $_param;
	private $_target;
	private $_propertyName='';
	private $_statement='';
	private $_loaded=false;
	private $_innerList;
	private $_connection;

	/**
	 * Create a new proxy list that will execute the mapped statement when any
	 * of the list's method are accessed for the first time.
	 * @param TMappedStatement statement to be executed to load the data.
	 * @param mixed parameter value for the statement.
	 * @param object result object that contains the lazy collection.
	 * @param string property of the result object to set the loaded collection.
	 */
	protected function __construct($mappedStatement, $param, $target, $propertyName)
	{
		$this->_param = $param;
		$this->_target = $target;
		$this->_statement = $mappedStatement;
		$this->_connection=$mappedStatement->getManager()->getDbConnection();
		$this->_propertyName = $propertyName;
	}

	/**
	 * Create a new instance of a lazy collection.
	 * @param TMappedStatement statement to be executed to load the data.
	 * @param mixed parameter value for the statement.
	 * @param object result object that contains the lazy collection.
	 * @param string property of the result object to set the loaded collection.
	 * @return TObjectProxy proxied collection object.
	 */
	public static function newInstance($mappedStatement, $param, $target, $propertyName)
	{
		$handler = new self($mappedStatement, $param, $target, $propertyName);
		$statement = $mappedStatement->getStatement();
		$registry=$mappedStatement->getManager()->getTypeHandlers();
		$list = $statement->createInstanceOfListClass($registry);
		if(!is_object($list))
			throw new TSqlMapExecutionException('sqlmap_invalid_lazyload_list',$statement->getID());
		return new TObjectProxy($handler, $list);
	}

	/**
	 * Relay the method call to the underlying collection.
	 * @param string method name.
	 * @param array method parameters.
	 */
	public function intercept($method, $arguments)
	{
		return call_user_func_array(array($this->_innerList, $method), $arguments);
	}

	/**
	 * Load the data by executing the mapped statement.
	 */
	protected function fetchListData()
	{
		if($this->_loaded == false)
		{
			$this->_innerList = $this->_statement->executeQueryForList($this->_connection,$this->_param);
			$this->_loaded = true;
			//replace the target property with real list
			TPropertyAccess::set($this->_target, $this->_propertyName, $this->_innerList);
		}
	}

	/**
	 * Try to fetch the data when any of the proxy collection method is called.
	 * @param string method name.
	 * @return boolean true if the underlying collection has the corresponding method name.
	 */
	public function hasMethod($method)
	{
		$this->fetchListData();
		if(is_object($this->_innerList))
			return in_array($method, get_class_methods($this->_innerList));
		return false;
	}
}

/**
 * TObjectProxy sets up a simple object that intercepts method calls to a
 * particular object and relays the call to handler object.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TLazyLoadList.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TObjectProxy
{
	private $_object;
	private $_handler;

	/**
	 * @param object handler to method calls.
	 * @param object the object to by proxied.
	 */
	public function __construct($handler, $object)
	{
		$this->_handler = $handler;
		$this->_object = $object;
	}

	/**
	 * Relay the method call to the handler object (if able to be handled), otherwise
	 * it calls the proxied object's method.
	 * @param string method name called
	 * @param array method arguments
	 * @return mixed method return value.
	 */
	public function __call($method,$params)
	{
		if($this->_handler->hasMethod($method))
			return $this->_handler->intercept($method, $params);
		else
			return call_user_func_array(array($this->_object, $method), $params);
	}
}

