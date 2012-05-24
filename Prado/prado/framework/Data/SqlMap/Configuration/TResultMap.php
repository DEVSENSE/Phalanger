<?php
/**
 * TResultMap class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TResultMap.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.SqlMap.Configuration
 */

/**
 * TResultMap corresponds to <resultMap> mapping tag.
 *
 * A TResultMap lets you control how data is extracted from the result of a
 * query, and how the columns are mapped to object properties. A TResultMap
 * can describe the column type, a null value replacement, and complex property
 * mappings including Collections.
 *
 * The <resultMap> can contain any number of property mappings that map object
 * properties to the columns of a result element. The property mappings are
 * applied, and the columns are read, in the order that they are defined.
 * Maintaining the element order ensures consistent results between different
 * drivers and providers.
 *
 * The {@link Class setClass()} property must be a PHP class object or array instance.
 *
 * The optional {@link Extends setExtends()} attribute can be set to the ID of
 * another <resultMap> upon which to base this <resultMap>. All properties of the
 * "parent" <resultMap> will be included as part of this <resultMap>, and values
 * from the "parent" <resultMap> are set before any values specified by this <resultMap>.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TResultMap.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TResultMap extends TComponent
{
	private $_columns;
	private $_class;
	private $_extends;
	private $_groupBy;
	private $_discriminator;
	private $_typeHandlers;
	private $_ID;

	/**
	 * Initialize the columns collection.
	 */
	public function __construct()
	{
		$this->_columns=new TMap;
	}

	/**
	 * @return string a unique identifier for the <resultMap>.
	 */
	public function getID()
	{
		return $this->_ID;
	}

	/**
	 * @param string a unique identifier for the <resultMap>.
	 */
	public function setID($value)
	{
		$this->_ID=$value;
	}

	/**
	 * @return string result class name.
	 */
	public function getClass()
	{
		return $this->_class;
	}

	/**
	 * @param string result class name.
	 */
	public function setClass($value)
	{
		$this->_class = $value;
	}

	/**
	 * @return TMap result columns.
	 */
	public function getColumns()
	{
		return $this->_columns;
	}

	/**
	 * @return string result map extends another result map.
	 */
	public function getExtends()
	{
		return $this->_extends;
	}

	/**
	 * @param string result map extends another result map.
	 */
	public function setExtends($value)
	{
		$this->_extends = $value;
	}

	/**
	 * @return string result map groups by.
	 */
	public function getGroupBy()
	{
		return $this->_groupBy;
	}

	/**
	 * @param string result map group by
	 */
	public function setGroupBy($value)
	{
		$this->_groupBy = $value;
	}

	/**
	 * @return TDiscriminator result class discriminator.
	 */
	public function getDiscriminator()
	{
		return $this->_discriminator;
	}

	/**
	 * @param TDiscriminator result class discriminator.
	 */
	public function setDiscriminator(TDiscriminator $value)
	{
		$this->_discriminator = $value;
	}

	/**
	 * Add a TResultProperty to result mapping.
	 * @param TResultProperty result property.
	 */
	public function addResultProperty(TResultProperty $property)
	{
		$this->_columns[$property->getProperty()] = $property;
	}

	/**
	 * Create a new instance of the class of this result map.
	 * @param TSqlMapTypeHandlerRegistry type handler registry.
	 * @return mixed new result object.
	 * @throws TSqlMapException
	 */
	public function createInstanceOfResult($registry)
	{
		$handler = $registry->getTypeHandler($this->getClass());
		try
		{
			if($handler!==null)
				return $handler->createNewInstance();
			else
				return $registry->createInstanceOf($this->getClass());
		}
		catch (TSqlMapException $e)
		{
			throw new TSqlMapException(
				'sqlmap_unable_to_create_new_instance',
					$this->getClass(), get_class($handler), $this->getID());
		}
	}

	/**
	 * Result sub-mappings using the discriminiator column.
	 * @param TSqlMapTypeHandlerRegistry type handler registry
	 * @param array row data.
	 * @return TResultMap result sub-map.
	 */
	public function resolveSubMap($registry,$row)
	{
		$subMap = $this;
		if(($disc = $this->getDiscriminator())!==null)
		{
			$value = $disc->getMapping()->getPropertyValue($registry,$row);
			$subMap = $disc->getSubMap((string)$value);

			if($subMap===null)
				$subMap = $this;
			else if($subMap !== $this)
				$subMap = $subMap->resolveSubMap($registry,$row);
		}
		return $subMap;
	}
}

