<?php
/**
 * TDiscriminator and TSubMap classes file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDiscriminator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Configuration
 */

/**
 * The TDiscriminator corresponds to the <discriminator> tag within a <resultMap>.
 *
 * TDiscriminator allows inheritance logic in SqlMap result mappings.
 * SqlMap compares the data found in the discriminator column to the different
 * <submap> values using the column value's string equivalence. When the string values
 * matches a particular <submap>, SqlMap will use the <resultMap> defined by
 * {@link resultMapping TSubMap::setResultMapping()} property for loading
 * the object data.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDiscriminator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TDiscriminator extends TComponent
{
	private $_column;
	private $_type;
	private $_typeHandler=null;
	private $_columnIndex;
	private $_nullValue;
	private $_mapping;
	private $_resultMaps=array();
	private $_subMaps=array();

	/**
	 * @return string the name of the column in the result set from which the
	 * value will be used to populate the property.
	 */
	public function getColumn()
	{
		return $this->_column;
	}

	/**
	 * @param string the name of the column in the result set from which the
	 * value will be used to populate the property.
	 */
	public function setColumn($value)
	{
		$this->_column = $value;
	}

	/**
	 * @param string property type of the parameter to be set.
	 */
	public function getType()
	{
		return $this->_type;
	}

	/**
	 * The type attribute is used to explicitly specify the property type of the
	 * parameter to be set. If the attribute type is not set and the framework
	 * cannot otherwise determine the type, the type is assumed from the default
	 * value of the property.
	 * @return string property type of the parameter to be set.
	 */
	public function setType($value)
	{
		$this->_type = $value;
	}

	/**
	 * @return string custom type handler class name (may use namespace).
	 */
	public function getTypeHandler()
	{
		return $this->_typeHandler;
	}

	/**
	 * @param string custom type handler class name (may use namespace).
	 */
	public function setTypeHandler($value)
	{
		$this->_typeHandler = $value;
	}

	/**
	 * @return int index of the column in the ResultSet
	 */
	public function getColumnIndex()
	{
		return $this->_columnIndex;
	}

	/**
	 * The columnIndex attribute value is the index of the column in the
	 * ResultSet from which the value will be used to populate the object property.
	 * @param int index of the column in the ResultSet
	 */
	public function setColumnIndex($value)
	{
		$this->_columnIndex = TPropertyValue::ensureInteger($value);
	}

	/**
	 * @return mixed outgoing null value replacement.
	 */
	public function getNullValue()
	{
		return $this->_nullValue;
	}

	/**
	 * @param mixed outgoing null value replacement.
	 */
	public function setNullValue($value)
	{
		$this->_nullValue = $value;
	}

	/**
	 * @return TResultProperty result property for the discriminator column.
	 */
	public function getMapping()
	{
		return $this->_mapping;
	}

	/**
	 * @param TSubMap add new sub mapping.
	 */
	public function addSubMap($subMap)
	{
		$this->_subMaps[] = $subMap;
	}

	/**
	 * @param string database value
	 * @return TResultMap result mapping.
	 */
	public function getSubMap($value)
	{
		if(isset($this->_resultMaps[$value]))
			return $this->_resultMaps[$value];
	}

	/**
	 * Copies the discriminator properties to a new TResultProperty.
	 * @param TResultMap result map holding the discriminator.
	 */
	public function initMapping($resultMap)
	{
		$this->_mapping = new TResultProperty($resultMap);
		$this->_mapping->setColumn($this->getColumn());
		$this->_mapping->setColumnIndex($this->getColumnIndex());
		$this->_mapping->setType($this->getType());
		$this->_mapping->setTypeHandler($this->getTypeHandler());
		$this->_mapping->setNullValue($this->getNullValue());
	}

	/**
	 * Set the result maps for particular sub-mapping values.
	 * @param TSqlMapManager sql map manager instance.
	 */
	public function initialize($manager)
	{
		foreach($this->_subMaps as $subMap)
		{
			$this->_resultMaps[$subMap->getValue()] =
				$manager->getResultMap($subMap->getResultMapping());
		}
	}
}

/**
 * TSubMap class defines a submapping value and the corresponding <resultMap>
 *
 * The {@link Value setValue()} property is used for comparison with the
 * discriminator column value. When the {@link Value setValue()} matches
 * that of the discriminator column value, the corresponding {@link ResultMapping setResultMapping}
 * is used inplace of the current result map.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDiscriminator.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSubMap extends TComponent
{
	private $_value;
	private $_resultMapping;

	/**
	 * @return string value for comparison with discriminator column value.
	 */
	public function getValue()
	{
		return $this->_value;
	}

	/**
	 * @param string value for comparison with discriminator column value.
	 */
	public function setValue($value)
	{
		$this->_value = $value;
	}

	/**
	 * The result map to use when the Value matches the discriminator column value.
	 * @return string ID of a result map
	 */
	public function getResultMapping()
	{
		return $this->_resultMapping;
	}

	/**
	 * @param string ID of a result map
	 */
	public function setResultMapping($value)
	{
		$this->_resultMapping = $value;
	}
}

