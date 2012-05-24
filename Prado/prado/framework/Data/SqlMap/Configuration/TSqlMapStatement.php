<?php
/**
 * TSqlMapStatement, TSqlMapInsert, TSqlMapUpdate, TSqlMapDelete,
 * TSqlMapSelect and TSqlMapSelectKey classes file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqlMapStatement.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Data.SqlMap.Configuration
 */

/**
 * TSqlMapStatement class corresponds to <statement> element.
 *
 * Mapped Statements can hold any SQL statement and can use Parameter Maps
 * and Result Maps for input and output.
 *
 * The <statement> element is a general "catch all" element that can be used
 * for any type of SQL statement. Generally it is a good idea to use one of the
 * more specific statement-type elements. The more specific elements provided
 * better error-checking and even more functionality. (For example, the insert
 * statement can return a database-generated key.)
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapStatement.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapStatement extends TComponent
{
	private $_parameterMapName;
	private $_parameterMap;
	private $_parameterClassName;
	private $_resultMapName;
	private $_resultMap;
	private $_resultClassName;
	private $_cacheModelName;
	private $_SQL;
	private $_listClass;
	private $_typeHandler;
	private $_extendStatement;
	private $_cache;
	private $_ID;

	/**
	 * @return string name for this statement, unique to each sql map manager.
	 */
	public function getID()
	{
		return $this->_ID;
	}

	/**
	 * @param string name for this statement, which must be unique for each sql map manager.
	 */
	public function setID($value)
	{
		$this->_ID=$value;
	}

	/**
	 * @return string name of a parameter map.
	 */
	public function getParameterMap()
	{
		return $this->_parameterMapName;
	}

	/**
	 * A Parameter Map defines an ordered list of values that match up with
	 * the "?" placeholders of a standard, parameterized query statement.
	 * @param string parameter map name.
	 */
	public function setParameterMap($value)
	{
		$this->_parameterMapName = $value;
	}

	/**
	 * @return string parameter class name.
	 */
	public function getParameterClass()
	{
		return $this->_parameterClassName;
	}

	/**
	 * If a {@link ParameterMap setParameterMap()} property is not specified,
	 * you may specify a ParameterClass instead and use inline parameters.
	 * The value of the parameterClass attribute can be any existing PHP class name.
	 * @param string parameter class name.
	 */
	public function setParameterClass($value)
	{
		$this->_parameterClassName = $value;
	}

	/**
	 * @return string result map name.
	 */
	public function getResultMap()
	{
		return $this->_resultMapName;
	}

	/**
	 * A Result Map lets you control how data is extracted from the result of a
	 * query, and how the columns are mapped to object properties.
	 * @param string result map name.
	 */
	public function setResultMap($value)
	{
		$this->_resultMapName = $value;
	}

	/**
	 * @return string result class name.
	 */
	public function getResultClass()
	{
		return $this->_resultClassName;
	}

	/**
	 * If a {@link ResultMap setResultMap()} is not specified, you may specify a
	 * ResultClass instead. The value of the ResultClass property can be the
	 * name of a PHP class or primitives like integer, string, or array. The
	 * class specified will be automatically mapped to the columns in the
	 * result, based on the result metadata.
	 * @param string result class name.
	 */
	public function setResultClass($value)
	{
		$this->_resultClassName = $value;
	}

	/**
	 * @return string cache mode name.
	 */
	public function getCacheModel()
	{
		return $this->_cacheModelName;
	}

	/**
	 * @param string cache mode name.
	 */
	public function setCacheModel($value)
	{
		$this->_cacheModelName = $value;
	}

	/**
	 * @return TSqlMapCacheModel cache implementation instance for this statement.
	 */
	public function getCache()
	{
		return $this->_cache;
	}

	/**
	 * @param TSqlMapCacheModel cache implementation instance for this statement.
	 */
	public function setCache($value)
	{
		$this->_cache = $value;
	}

	/**
	 * @return TStaticSql sql text container.
	 */
	public function getSqlText()
	{
		return $this->_SQL;
	}

	/**
	 * @param TStaticSql sql text container.
	 */
	public function setSqlText($value)
	{
		$this->_SQL = $value;
	}

	/**
	 * @return string name of a PHP class that implements ArrayAccess.
	 */
	public function getListClass()
	{
		return $this->_listClass;
	}

	/**
	 * An ArrayAccess class can be specified to handle the type of objects in the collection.
	 * @param string name of a PHP class that implements ArrayAccess.
	 */
	public function setListClass($value)
	{
		$this->_listClass = $value;
	}

	/**
	 * @return string another statement element name.
	 */
	public function getExtends()
	{
		return $this->_extendStatement;
	}

	/**
	 * @param string name of another statement element to extend.
	 */
	public function setExtends($value)
	{
		$this->_extendStatement = $value;
	}

	/**
	 * @return TResultMap the result map corresponding to the
	 * {@link ResultMap getResultMap()} property.
	 */
	public function resultMap()
	{
		return $this->_resultMap;
	}

	/**
	 * @return TParameterMap the parameter map corresponding to the
	 * {@link ParameterMap getParameterMap()} property.
	 */
	public function parameterMap()
	{
		return $this->_parameterMap;
	}

	/**
	 * @param TInlineParameterMap parameter extracted from the sql text.
	 */
	public function setInlineParameterMap($map)
	{
		$this->_parameterMap = $map;
	}

	/**
	 * @param TSqlMapManager initialize the statement, sets the result and parameter maps.
	 */
	public function initialize($manager)
	{
		if(strlen($this->_resultMapName) > 0)
			$this->_resultMap = $manager->getResultMap($this->_resultMapName);
		if(strlen($this->_parameterMapName) > 0)
			$this->_parameterMap = $manager->getParameterMap($this->_parameterMapName);
	}

	/**
	 * @param TSqlMapTypeHandlerRegistry type handler registry
	 * @return ArrayAccess new instance of list class.
	 */
	public function createInstanceOfListClass($registry)
	{
		if(strlen($type = $this->getListClass()) > 0)
			return $this->createInstanceOf($registry,$type);
		return array();
	}

	/**
	 * Create a new instance of a given type.
	 * @param TSqlMapTypeHandlerRegistry type handler registry
	 * @param string result class name.
	 * @param array result data.
	 * @return mixed result object.
	 */
	protected function createInstanceOf($registry,$type,$row=null)
	{
		$handler = $registry->getTypeHandler($type);
		if($handler!==null)
			return $handler->createNewInstance($row);
		else
			return $registry->createInstanceOf($type);
	}

	/**
	 * Create a new instance of result class.
	 * @param TSqlMapTypeHandlerRegistry type handler registry
	 * @param array result data.
	 * @return mixed result object.
	 */
	public function createInstanceOfResultClass($registry,$row)
	{
		if(strlen($type= $this->getResultClass()) > 0)
			return $this->createInstanceOf($registry,$type,$row);
	}
}

/**
 * TSqlMapSelect class file.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapStatement.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Data.SqlMap.Statements
 * @since 3.1
 */
class TSqlMapSelect extends TSqlMapStatement
{
	private $_generate;

	public function getGenerate(){ return $this->_generate; }
	public function setGenerate($value){ $this->_generate = $value; }
}

/**
 * TSqlMapInsert class corresponds to the <insert> element.
 *
 * The <insert> element allows <selectKey> child elements that can be used
 * to generate a key to be used for the insert command.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapStatement.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapInsert extends TSqlMapStatement
{
	private $_selectKey=null;

	/**
	 * @return TSqlMapSelectKey select key element.
	 */
	public function getSelectKey()
	{
		return $this->_selectKey;
	}

	/**
	 * @param TSqlMapSelectKey select key.
	 */
	public function setSelectKey($value)
	{
		$this->_selectKey = $value;
	}
}

/**
 * TSqlMapUpdate class corresponds to <update> element.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapStatement.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapUpdate extends TSqlMapStatement
{
}

/**
 * TSqlMapDelete class corresponds to the <delete> element.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapStatement.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapDelete extends TSqlMapUpdate
{
}

/**
 * TSqlMapSelect corresponds to the <selectKey> element.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapStatement.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapSelectKey extends TSqlMapStatement
{
	private $_type = 'post';
	private $_property;

	/**
	 * @return string select generated key type, 'post' or 'pre'.
	 */
	public function getType()
	{
		return $this->_type;
	}

	/**
	 * @param string select generated key type, 'post' or 'pre'.
	 */
	public function setType($value)
	{
		$this->_type = strtolower($value) == 'post' ? 'post' : 'pre';
	}

	/**
	 * @return string property name for the generated key.
	 */
	public function getProperty()
	{
		return $this->_property;
	}

	/**
	 * @param string property name for the generated key.
	 */
	public function setProperty($value)
	{
		$this->_property = $value;
	}

	/**
	 * @throws TSqlMapConfigurationException extends is unsupported.
	 */
	public function setExtends($value)
	{
		throw new TSqlMapConfigurationException('sqlmap_can_not_extend_select_key');
	}

	/**
	 * @return boolean true if key is generated after insert command, false otherwise.
	 */
	public function getIsAfter()
	{
		return $this->_type == 'post';
	}
}

