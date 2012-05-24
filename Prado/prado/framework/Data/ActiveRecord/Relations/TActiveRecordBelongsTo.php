<?php
/**
 * TActiveRecordBelongsTo class file.
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
 * Implements the foreign key relationship (TActiveRecord::BELONGS_TO) between
 * the source objects and the related foreign object. Consider the
 * <b>entity</b> relationship between a Team and a Player.
 * <code>
 * +------+            +--------+
 * | Team | 1 <----- * | Player |
 * +------+            +--------+
 * </code>
 * Where one team may have 0 or more players and each player belongs to only
 * one team. We may model Team-Player <b>object</b> relationship as active record as follows.
 * <code>
 * class TeamRecord extends TActiveRecord
 * {
 *     // see TActiveRecordHasMany for detailed definition.
 * }
 * class PlayerRecord extends TActiveRecord
 * {
 *     const TABLE='player';
 *     public $player_id; //primary key
 *     public $team_name; //foreign key player.team_name <-> team.name
 * 	   public $age;
 *     public $team; //foreign object TeamRecord
 *
 *     public static $RELATIONS = array
 *     (
 *			'team' => array(self::BELONGS_TO, 'TeamRecord')
 *     );
 *
 *	   public static function finder($className=__CLASS__)
 *	   {
 *		   return parent::finder($className);
 *	   }
 * }
 * </code>
 * The static <tt>$RELATIONS</tt> property of PlayerRecord defines that the
 * property <tt>$team</tt> belongs to a <tt>TeamRecord</tt>.
 *
 * The team object may be fetched as follows.
 * <code>
 * $players = PlayerRecord::finder()->with_team()->findAll();
 * </code>
 * The method <tt>with_xxx()</tt> (where <tt>xxx</tt> is the relationship property
 * name, in this case, <tt>team</tt>) fetchs the corresponding TeamRecords using
 * a second query (not by using a join). The <tt>with_xxx()</tt> accepts the same
 * arguments as other finder methods of TActiveRecord, e.g.
 * <tt>with_team('location = ?', 'Madrid')</tt>.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id$
 * @package System.Data.ActiveRecord.Relations
 * @since 3.1
 */
class TActiveRecordBelongsTo extends TActiveRecordRelation
{
	/**
	 * Get the foreign key index values from the results and make calls to the
	 * database to find the corresponding foreign objects.
	 * @param array original results.
	 */
	protected function collectForeignObjects(&$results)
	{
		$fkeys = $this->getRelationForeignKeys();

		$properties = array_keys($fkeys);
		$fields = array_values($fkeys);
		$indexValues = $this->getIndexValues($properties, $results);
		$fkObjects = $this->findForeignObjects($fields, $indexValues);
		$this->populateResult($results,$properties,$fkObjects,$fields);
	}

	/**
	 * @return array foreign key field names as key and object properties as value.
	 * @since 3.1.2
	 */
	public function getRelationForeignKeys()
	{
		$fkObject = $this->getContext()->getForeignRecordFinder();
		return $this->findForeignKeys($this->getSourceRecord(),$fkObject);
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
			$source->$prop=$collections[$hash][0];
		}
		else
			$source->$prop=null;
	}

	/**
	 * Updates the source object first.
	 * @return boolean true if all update are success (including if no update was required), false otherwise .
	 */
	public function updateAssociatedRecords()
	{
		$obj = $this->getContext()->getSourceRecord();
		$fkObject = $obj->getColumnValue($this->getContext()->getProperty());
		if($fkObject!==null)
		{
			$fkObject->save();
			$source = $this->getSourceRecord();
			$fkeys = $this->findForeignKeys($source, $fkObject);
			foreach($fkeys as $srcKey => $fKey)
				$source->setColumnValue($srcKey, $fkObject->getColumnValue($fKey));
			return true;
		}
		return false;
	}
}

