<?php
/**
 * TSqlMapTypeHandlerRegistry, and abstract TSqlMapTypeHandler classes file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqlMapTypeHandlerRegistry.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 */

/**
 * TTypeHandlerFactory provides type handler classes to convert database field type
 * to PHP types and vice versa.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapTypeHandlerRegistry.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TSqlMapTypeHandlerRegistry
{
	private $_typeHandlers=array();

	/**
	 * @param string database field type
	 * @return TSqlMapTypeHandler type handler for give database field type.
	 */
	public function getDbTypeHandler($dbType='NULL')
	{
		foreach($this->_typeHandlers as $handler)
			if($handler->getDbType()===$dbType)
				return $handler;
	}

	/**
	 * @param string type handler class name
	 * @return TSqlMapTypeHandler type handler
	 */
	public function getTypeHandler($class)
	{
		if(isset($this->_typeHandlers[$class]))
			return $this->_typeHandlers[$class];
	}

	/**
	 * @param TSqlMapTypeHandler registers a new type handler
	 */
	public function registerTypeHandler(TSqlMapTypeHandler $handler)
	{
		$this->_typeHandlers[$handler->getType()] = $handler;
	}

	/**
	 * Creates a new instance of a particular class (for PHP primative types,
	 * their corresponding default value for given type is used).
	 * @param string PHP type name
	 * @return mixed default type value, if no type is specified null is returned.
	 * @throws TSqlMapException if class name is not found.
	 */
	public function createInstanceOf($type='')
	{
		if(strlen($type) > 0)
		{
			switch(strtolower($type))
			{
				case 'string': return '';
				case 'array': return array();
				case 'float': case 'double': case 'decimal': return 0.0;
				case 'integer': case 'int': return 0;
				case 'bool': case 'boolean': return false;
			}

			if(class_exists('Prado', false))
				return Prado::createComponent($type);
			else if(class_exists($type, false)) //NO auto loading
				return new $type;
			else
				throw new TSqlMapException('sqlmap_unable_to_find_class', $type);
		}
	}

	/**
	 * Converts the value to given type using PHP's settype() function.
	 * @param string PHP primative type.
	 * @param mixed value to be casted
	 * @return mixed type casted value.
	 */
	public function convertToType($type, $value)
	{
		switch(strtolower($type))
		{
			case 'integer': case 'int':
				$type = 'integer'; break;
			case 'float': case 'double': case 'decimal':
				$type = 'float'; break;
			case 'boolean': case 'bool':
				$type = 'boolean'; break;
			case 'string' :
				$type = 'string'; break;
			default:
				return $value;
		}
		settype($value, $type);
		return $value;
	}
}

/**
 * A simple interface for implementing custom type handlers.
 *
 * Using this interface, you can implement a type handler that
 * will perform customized processing before parameters are set
 * on and after values are retrieved from the database.
 * Using a custom type handler you can extend
 * the framework to handle types that are not supported, or
 * handle supported types in a different way.  For example,
 * you might use a custom type handler to implement proprietary
 * BLOB support (e.g. Oracle), or you might use it to handle
 * booleans using "Y" and "N" instead of the more typical 0/1.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapTypeHandlerRegistry.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
abstract class TSqlMapTypeHandler extends TComponent
{
	private $_dbType='NULL';
	private $_type;
	/**
	 * @param string database field type.
	 */
	public function setDbType($value)
	{
		$this->_dbType=$value;
	}

	/**
	 * @return string database field type.
	 */
	public function getDbType()
	{
		return $this->_dbType;
	}

	public function getType()
	{
		if($this->_type===null)
			return get_class($this);
		else
			return $this->_type;
	}

	public function setType($value)
	{
		$this->_type=$value;
	}

	/**
	 * Performs processing on a value before it is used to set
	 * the parameter of a IDbCommand.
	 * @param object The interface for setting the value.
	 * @param object The value to be set.
	 */
	public abstract function getParameter($object);


	/**
	 * Performs processing on a value before after it has been retrieved
	 * from a database
	 * @param object The interface for getting the value.
	 * @return mixed The processed value.
	 */
	public abstract function getResult($string);


	/**
	 * Casts the string representation of a value into a type recognized by
	 * this type handler.  This method is used to translate nullValue values
	 * into types that can be appropriately compared.  If your custom type handler
	 * cannot support nullValues, or if there is no reasonable string representation
	 * for this type (e.g. File type), you can simply return the String representation
	 * as it was passed in.  It is not recommended to return null, unless null was passed
	 * in.
	 * @param array result row.
	 * @return mixed
	 */
	public abstract function createNewInstance($row=null);
}

