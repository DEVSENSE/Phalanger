<?php
/**
 * TActiveRecordRelation class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Data.ActiveRecord.Relations
 */

/**
 * Load active record relationship context.
 */
Prado::using('System.Data.ActiveRecord.Relations.TActiveRecordRelationContext');

/**
 * Base class for active record relationships.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id$
 * @package System.Data.ActiveRecord.Relations
 * @since 3.1
 */
abstract class TActiveRecordRelation
{
	private $_context;
	private $_criteria;

	public function __construct(TActiveRecordRelationContext $context, $criteria)
	{
		$this->_context = $context;
		$this->_criteria = $criteria;
	}

	/**
	 * @return TActiveRecordRelationContext
	 */
	protected function getContext()
	{
		return $this->_context;
	}

	/**
	 * @return TActiveRecordCriteria
	 */
	protected function getCriteria()
	{
		return $this->_criteria;
	}

	/**
	 * @return TActiveRecord
	 */
	protected function getSourceRecord()
	{
		return $this->getContext()->getSourceRecord();
	}

	abstract protected function collectForeignObjects(&$results);

	/**
	 * Dispatch the method calls to the source record finder object. When
	 * an instance of TActiveRecord or an array of TActiveRecord is returned
	 * the corresponding foreign objects are also fetched and assigned.
	 *
	 * Multiple relationship calls can be chain together.
	 *
	 * @param string method name called
	 * @param array method arguments
	 * @return mixed TActiveRecord or array of TActiveRecord results depending on the method called.
	 */
	public function __call($method,$args)
	{
		static $stack=array();

		$results = call_user_func_array(array($this->getSourceRecord(),$method),$args);
		$validArray = is_array($results) && count($results) > 0;
		if($validArray || $results instanceof ArrayAccess || $results instanceof TActiveRecord)
		{
			$this->collectForeignObjects($results);
			while($obj = array_pop($stack))
				$obj->collectForeignObjects($results);
		}
		else if($results instanceof TActiveRecordRelation)
			$stack[] = $this; //call it later
		else if($results === null || !$validArray)
			$stack = array();
		return $results;
	}

	/**
	 * Fetch results for current relationship.
	 * @return boolean always true.
	 */
	public function fetchResultsInto($obj)
	{
		$this->collectForeignObjects($obj);
		return true;
	}

	/**
	 * Returns foreign keys in $fromRecord with source column names as key
	 * and foreign column names in the corresponding $matchesRecord as value.
	 * The method returns the first matching foreign key between these 2 records.
	 * @param TActiveRecord $fromRecord
	 * @param TActiveRecord $matchesRecord
	 * @return array foreign keys with source column names as key and foreign column names as value.
	 */
	protected function findForeignKeys($from, $matchesRecord, $loose=false)
	{
		$gateway = $matchesRecord->getRecordGateway();
		$recordTableInfo = $gateway->getRecordTableInfo($matchesRecord);
		$matchingTableName = strtolower($recordTableInfo->getTableName());
		$matchingFullTableName = strtolower($recordTableInfo->getTableFullName());
		$tableInfo=$from;
		if($from instanceof TActiveRecord)
			$tableInfo = $gateway->getRecordTableInfo($from);
		//find first non-empty FK
		foreach($tableInfo->getForeignKeys() as $fkeys)
		{
			$fkTable = strtolower($fkeys['table']);
			if($fkTable===$matchingTableName || $fkTable===$matchingFullTableName)
			{
				$hasFkField = !$loose && $this->getContext()->hasFkField();
				$key = $hasFkField ? $this->getFkFields($fkeys['keys']) : $fkeys['keys'];
				if(!empty($key))
					return $key;
			}
		}

		//none found
		$matching = $gateway->getRecordTableInfo($matchesRecord)->getTableFullName();
		throw new TActiveRecordException('ar_relations_missing_fk',
			$tableInfo->getTableFullName(), $matching);
	}

	/**
	 * @return array foreign key field names as key and object properties as value.
	 * @since 3.1.2
	 */
	abstract public function getRelationForeignKeys();

	/**
	 * Find matching foreign key fields from the 3rd element of an entry in TActiveRecord::$RELATION.
	 * Assume field names consist of [\w-] character sets. Prefix to the field names ending with a dot
	 * are ignored.
	 */
	private function getFkFields($fkeys)
	{
		$matching = array();
		preg_match_all('/\s*(\S+\.)?([\w-]+)\s*/', $this->getContext()->getFkField(), $matching);
		$fields = array();
		foreach($fkeys as $fkName => $field)
		{
			if(in_array($fkName, $matching[2]))
				$fields[$fkName] = $field;
		}
		return $fields;
	}

	/**
	 * @param mixed object or array to be hashed
	 * @param array name of property for hashing the properties.
	 * @return string object hash using crc32 and serialize.
	 */
	protected function getObjectHash($obj, $properties)
	{
		$ids=array();
		foreach($properties as $property)
			$ids[] = is_object($obj) ? (string)$obj->getColumnValue($property) : (string)$obj[$property];
		return serialize($ids);
	}

	/**
	 * Fetches the foreign objects using TActiveRecord::findAllByIndex()
	 * @param array field names
	 * @param array foreign key index values.
	 * @return TActiveRecord[] foreign objects.
	 */
	protected function findForeignObjects($fields, $indexValues)
	{
		$finder = $this->getContext()->getForeignRecordFinder();
		return $finder->findAllByIndex($this->_criteria, $fields, $indexValues);
	}

	/**
	 * Obtain the foreign key index values from the results.
	 * @param array property names
	 * @param array TActiveRecord results
	 * @return array foreign key index values.
	 */
	protected function getIndexValues($keys, $results)
	{
		if(!is_array($results) && !$results instanceof ArrayAccess)
			$results = array($results);
		$values=array();
		foreach($results as $result)
		{
			$value = array();
			foreach($keys as $name)
				$value[] = $result->getColumnValue($name);
			$values[] = $value;
		}
		return $values;
	}

	/**
	 * Populate the results with the foreign objects found.
	 * @param array source results
	 * @param array source property names
	 * @param array foreign objects
	 * @param array foreign object field names.
	 */
	protected function populateResult(&$results,$properties,&$fkObjects,$fields)
	{
		$collections=array();
		foreach($fkObjects as $fkObject)
			$collections[$this->getObjectHash($fkObject, $fields)][]=$fkObject;
		$this->setResultCollection($results, $collections, $properties);
	}

	/**
	 * Populates the result array with foreign objects (matched using foreign key hashed property values).
	 * @param array $results
	 * @param array $collections
	 * @param array property names
	 */
	protected function setResultCollection(&$results, &$collections, $properties)
	{
		if(is_array($results) || $results instanceof ArrayAccess)
		{
			for($i=0,$k=count($results);$i<$k;$i++)
				$this->setObjectProperty($results[$i], $properties, $collections);
		}
		else
			$this->setObjectProperty($results, $properties, $collections);
	}

	/**
	 * Sets the foreign objects to the given property on the source object.
	 * @param TActiveRecord source object.
	 * @param array source properties
	 * @param array foreign objects.
	 */
	protected function setObjectProperty($source, $properties, &$collections)
	{
		$hash = $this->getObjectHash($source, $properties);
		$prop = $this->getContext()->getProperty();
		$source->$prop=isset($collections[$hash]) ? $collections[$hash] : array();
	}
}

