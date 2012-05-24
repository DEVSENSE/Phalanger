<?php
/**
 * TActiveRecordHasOne class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Data.ActiveRecord.Relations
 */

/**
 * Loads base active record relationship class.
 */
Prado::using('System.Data.ActiveRecord.Relations.TActiveRecordRelation');

/**
 * TActiveRecordHasOne models the object relationship that a record (the source object)
 * property is an instance of foreign record object having a foreign key
 * related to the source object. The HAS_ONE relation is very similar to the
 * HAS_MANY relationship (in fact, it is equivalent in the entities relationship point of view).
 *
 * The difference of HAS_ONE from HAS_MANY is that the foreign object is singular.
 * That is, HAS_MANY will return a collection of records while HAS_ONE returns the
 * corresponding record.
 *
 * Consider the <b>entity</b> relationship between a Car and a Engine.
 * <code>
 * +-----+            +--------+
 * | Car | 1 <----- 1 | Engine |
 * +-----+            +--------+
 * </code>
 * Where each engine belongs to only one car, that is, the Engine entity has
 * a foreign key to the Car's primary key. We may model
 * Engine-Car <b>object</b> relationship as active record as follows.
 * <code>
 * class CarRecord extends TActiveRecord
 * {
 *     const TABLE='car';
 *     public $car_id; //primary key
 *     public $colour;
 *
 *     public $engine; //engine foreign object
 *
 *     public static $RELATIONS=array
 *     (
 *         'engine' => array(self::HAS_ONE, 'EngineRecord')
 *     );
 *
 *	   public static function finder($className=__CLASS__)
 *	   {
 *		   return parent::finder($className);
 *	   }
 * }
 * class EngineRecord extends TActiveRecord
 * {
 *     const TABLE='engine';
 *     public $engine_id;
 *     public $capacity;
 *     public $car_id; //foreign key to cars
 *
 *	   public static function finder($className=__CLASS__)
 *	   {
 *		   return parent::finder($className);
 *	   }
 * }
 * </code>
 * The static <tt>$RELATIONS</tt> property of CarRecord defines that the
 * property <tt>$engine</tt> that will reference an <tt>EngineRecord</tt> instance.
 *
 * The car record with engine property list may be fetched as follows.
 * <code>
 * $cars = CarRecord::finder()->with_engine()->findAll();
 * </code>
 * The method <tt>with_xxx()</tt> (where <tt>xxx</tt> is the relationship property
 * name, in this case, <tt>engine</tt>) fetchs the corresponding EngineRecords using
 * a second query (not by using a join). The <tt>with_xxx()</tt> accepts the same
 * arguments as other finder methods of TActiveRecord, e.g. <tt>with_engine('capacity < ?', 3.8)</tt>.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id$
 * @package System.Data.ActiveRecord.Relations
 * @since 3.1
 */
class TActiveRecordHasOne extends TActiveRecordRelation
{
	/**
	 * Get the foreign key index values from the results and make calls to the
	 * database to find the corresponding foreign objects.
	 * @param array original results.
	 */
	protected function collectForeignObjects(&$results)
	{
		$fkeys = $this->getRelationForeignKeys();
		$properties = array_values($fkeys);
		$fields = array_keys($fkeys);

		$indexValues = $this->getIndexValues($properties, $results);
		$fkObjects = $this->findForeignObjects($fields,$indexValues);
		$this->populateResult($results,$properties,$fkObjects,$fields);
	}

	/**
	 * @return array foreign key field names as key and object properties as value.
	 * @since 3.1.2
	 */
	public function getRelationForeignKeys()
	{
		$fkObject = $this->getContext()->getForeignRecordFinder();
		return $this->findForeignKeys($fkObject, $this->getSourceRecord());
	}

	/**
	 * Sets the foreign objects to the given property on the source object.
	 * @param TActiveRecord source object.
	 * @param array foreign objects.
	 */
	protected function setObjectProperty($source, $properties, &$collections)
	{
		$hash = $this->getObjectHash($source, $properties);
		$prop = $this->getContext()->getProperty();
		if(isset($collections[$hash]) && count($collections[$hash]) > 0)
		{
			if(count($collections[$hash]) > 1)
				throw new TActiveRecordException('ar_belongs_to_multiple_result');
			$source->setColumnValue($prop, $collections[$hash][0]);
		}
	}

	/**
	 * Updates the associated foreign objects.
	 * @return boolean true if all update are success (including if no update was required), false otherwise .
	 */
	public function updateAssociatedRecords()
	{
		$fkObject = $this->getContext()->getPropertyValue();
		$source = $this->getSourceRecord();
		$fkeys = $this->findForeignKeys($fkObject, $source);
		foreach($fkeys as $fKey => $srcKey)
			$fkObject->setColumnValue($fKey, $source->getColumnValue($srcKey));
		return $fkObject->save();
	}
}

