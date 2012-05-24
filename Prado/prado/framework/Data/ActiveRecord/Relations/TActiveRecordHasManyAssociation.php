<?php
/**
 * TActiveRecordHasManyAssociation class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Data.ActiveRecord.Relations
 */

/**
 * Loads base active record relations class.
 */
Prado::using('System.Data.ActiveRecord.Relations.TActiveRecordRelation');

/**
 * Implements the M-N (many to many) relationship via association table.
 * Consider the <b>entity</b> relationship between Articles and Categories
 * via the association table <tt>Article_Category</tt>.
 * <code>
 * +---------+            +------------------+            +----------+
 * | Article | * -----> * | Article_Category | * <----- * | Category |
 * +---------+            +------------------+            +----------+
 * </code>
 * Where one article may have 0 or more categories and each category may have 0
 * or more articles. We may model Article-Category <b>object</b> relationship
 * as active record as follows.
 * <code>
 * class ArticleRecord
 * {
 *     const TABLE='Article';
 *     public $article_id;
 *
 *     public $Categories=array(); //foreign object collection.
 *
 *     public static $RELATIONS = array
 *     (
 *         'Categories' => array(self::MANY_TO_MANY, 'CategoryRecord', 'Article_Category')
 *     );
 *
 *     public static function finder($className=__CLASS__)
 *     {
 *         return parent::finder($className);
 *     }
 * }
 * class CategoryRecord
 * {
 *     const TABLE='Category';
 *     public $category_id;
 *
 *     public $Articles=array();
 *
 *     public static $RELATIONS = array
 *     (
 *         'Articles' => array(self::MANY_TO_MANY, 'ArticleRecord', 'Article_Category')
 *     );
 *
 *     public static function finder($className=__CLASS__)
 *     {
 *         return parent::finder($className);
 *     }
 * }
 * </code>
 *
 * The static <tt>$RELATIONS</tt> property of ArticleRecord defines that the
 * property <tt>$Categories</tt> has many <tt>CategoryRecord</tt>s. Similar, the
 * static <tt>$RELATIONS</tt> property of CategoryRecord defines many ArticleRecords.
 *
 * The articles with categories list may be fetched as follows.
 * <code>
 * $articles = TeamRecord::finder()->withCategories()->findAll();
 * </code>
 * The method <tt>with_xxx()</tt> (where <tt>xxx</tt> is the relationship property
 * name, in this case, <tt>Categories</tt>) fetchs the corresponding CategoryRecords using
 * a second query (not by using a join). The <tt>with_xxx()</tt> accepts the same
 * arguments as other finder methods of TActiveRecord.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id$
 * @package System.Data.ActiveRecord.Relations
 * @since 3.1
 */
class TActiveRecordHasManyAssociation extends TActiveRecordRelation
{
	private $_association;
	private $_sourceTable;
	private $_foreignTable;
	private $_association_columns=array();

	/**
	* Get the foreign key index values from the results and make calls to the
	* database to find the corresponding foreign objects using association table.
	* @param array original results.
	*/
	protected function collectForeignObjects(&$results)
	{
		list($sourceKeys, $foreignKeys) = $this->getRelationForeignKeys();
		$properties = array_values($sourceKeys);
		$indexValues = $this->getIndexValues($properties, $results);
		$this->fetchForeignObjects($results, $foreignKeys,$indexValues,$sourceKeys);
	}

	/**
	 * @return array 2 arrays of source keys and foreign keys from the association table.
	 */
	public function getRelationForeignKeys()
	{
		$association = $this->getAssociationTable();
		$sourceKeys = $this->findForeignKeys($association, $this->getSourceRecord(), true);
		$fkObject = $this->getContext()->getForeignRecordFinder();
		$foreignKeys = $this->findForeignKeys($association, $fkObject);
		return array($sourceKeys, $foreignKeys);
	}

	/**
	 * @return TDbTableInfo association table information.
	 */
	protected function getAssociationTable()
	{
		if($this->_association===null)
		{
			$gateway = $this->getSourceRecord()->getRecordGateway();
			$conn = $this->getSourceRecord()->getDbConnection();
			//table name may include the fk column name separated with a dot.
			$table = explode('.', $this->getContext()->getAssociationTable());
			if(count($table)>1)
			{
				$columns = preg_replace('/^\((.*)\)/', '\1', $table[1]);
				$this->_association_columns = preg_split('/\s*[, ]\*/',$columns);
			}
			$this->_association = $gateway->getTableInfo($conn, $table[0]);
		}
		return $this->_association;
	}

	/**
	 * @return TDbTableInfo source table information.
	 */
	protected function getSourceTable()
	{
		if($this->_sourceTable===null)
		{
			$gateway = $this->getSourceRecord()->getRecordGateway();
			$this->_sourceTable = $gateway->getRecordTableInfo($this->getSourceRecord());
		}
		return $this->_sourceTable;
	}

	/**
	 * @return TDbTableInfo foreign table information.
	 */
	protected function getForeignTable()
	{
		if($this->_foreignTable===null)
		{
			$gateway = $this->getSourceRecord()->getRecordGateway();
			$fkObject = $this->getContext()->getForeignRecordFinder();
			$this->_foreignTable = $gateway->getRecordTableInfo($fkObject);
		}
		return $this->_foreignTable;
	}

	/**
	 * @return TDataGatewayCommand
	 */
	protected function getCommandBuilder()
	{
		return $this->getSourceRecord()->getRecordGateway()->getCommand($this->getSourceRecord());
	}

	/**
	 * @return TDataGatewayCommand
	 */
	protected function getForeignCommandBuilder()
	{
		$obj = $this->getContext()->getForeignRecordFinder();
		return $this->getSourceRecord()->getRecordGateway()->getCommand($obj);
	}


	/**
	 * Fetches the foreign objects using TActiveRecord::findAllByIndex()
	 * @param array field names
	 * @param array foreign key index values.
	 */
	protected function fetchForeignObjects(&$results,$foreignKeys,$indexValues,$sourceKeys)
	{
		$criteria = $this->getCriteria();
		$finder = $this->getContext()->getForeignRecordFinder();
		$type = get_class($finder);
		$command = $this->createCommand($criteria, $foreignKeys,$indexValues,$sourceKeys);
		$srcProps = array_keys($sourceKeys);
		$collections=array();
		foreach($this->getCommandBuilder()->onExecuteCommand($command, $command->query()) as $row)
		{
			$hash = $this->getObjectHash($row, $srcProps);
			foreach($srcProps as $column)
				unset($row[$column]);
			$obj = $this->createFkObject($type,$row,$foreignKeys);
			$collections[$hash][] = $obj;
		}
		$this->setResultCollection($results, $collections, array_values($sourceKeys));
	}

	/**
	 * @param string active record class name.
	 * @param array row data
	 * @param array foreign key column names
	 * @return TActiveRecord
	 */
	protected function createFkObject($type,$row,$foreignKeys)
	{
		$obj = TActiveRecord::createRecord($type, $row);
		if(count($this->_association_columns) > 0)
		{
			$i=0;
			foreach($foreignKeys as $ref=>$fk)
				$obj->setColumnValue($ref, $row[$this->_association_columns[$i++]]);
		}
		return $obj;
	}

	/**
	 * @param TSqlCriteria
	 * @param TTableInfo association table info
	 * @param array field names
	 * @param array field values
	 */
	public function createCommand($criteria, $foreignKeys,$indexValues,$sourceKeys)
	{
		$innerJoin = $this->getAssociationJoin($foreignKeys,$indexValues,$sourceKeys);
		$fkTable = $this->getForeignTable()->getTableFullName();
		$srcColumns = $this->getSourceColumns($sourceKeys);
		if(($where=$criteria->getCondition())===null)
			$where='1=1';
		$sql = "SELECT {$fkTable}.*, {$srcColumns} FROM {$fkTable} {$innerJoin} WHERE {$where}";

		$parameters = $criteria->getParameters()->toArray();
		$ordering = $criteria->getOrdersBy();
		$limit = $criteria->getLimit();
		$offset = $criteria->getOffset();

		$builder = $this->getForeignCommandBuilder()->getBuilder();
		$command = $builder->applyCriterias($sql,$parameters,$ordering,$limit,$offset);
		$this->getCommandBuilder()->onCreateCommand($command, $criteria);
		return $command;
	}

	/**
	 * @param array source table column names.
	 * @return string comma separated source column names.
	 */
	protected function getSourceColumns($sourceKeys)
	{
		$columns=array();
		$table = $this->getAssociationTable();
		$tableName = $table->getTableFullName();
		$columnNames = array_merge(array_keys($sourceKeys),$this->_association_columns);
		foreach($columnNames as $name)
			$columns[] = $tableName.'.'.$table->getColumn($name)->getColumnName();
		return implode(', ', $columns);
	}

	/**
	 * SQL inner join for M-N relationship via association table.
	 * @param array foreign table column key names.
	 * @param array source table index values.
	 * @param array source table column names.
	 * @return string inner join condition for M-N relationship via association table.
	 */
	protected function getAssociationJoin($foreignKeys,$indexValues,$sourceKeys)
	{
		$refInfo= $this->getAssociationTable();
		$fkInfo = $this->getForeignTable();

		$refTable = $refInfo->getTableFullName();
		$fkTable = $fkInfo->getTableFullName();

		$joins = array();
		$hasAssociationColumns = count($this->_association_columns) > 0;
		$i=0;
		foreach($foreignKeys as $ref=>$fk)
		{
			if($hasAssociationColumns)
				$refField = $refInfo->getColumn($this->_association_columns[$i++])->getColumnName();
			else
				$refField = $refInfo->getColumn($ref)->getColumnName();
			$fkField = $fkInfo->getColumn($fk)->getColumnName();
			$joins[] = "{$fkTable}.{$fkField} = {$refTable}.{$refField}";
		}
		$joinCondition = implode(' AND ', $joins);
		$index = $this->getCommandBuilder()->getIndexKeyCondition($refInfo,array_keys($sourceKeys), $indexValues);
		return "INNER JOIN {$refTable} ON ({$joinCondition}) AND {$index}";
	}

	/**
	 * Updates the associated foreign objects.
	 * @return boolean true if all update are success (including if no update was required), false otherwise .
	 */
	public function updateAssociatedRecords()
	{
		$obj = $this->getContext()->getSourceRecord();
		$fkObjects = &$obj->{$this->getContext()->getProperty()};
		$success=true;
		if(($total = count($fkObjects))> 0)
		{
			$source = $this->getSourceRecord();
			$builder = $this->getAssociationTableCommandBuilder();
			for($i=0;$i<$total;$i++)
				$success = $fkObjects[$i]->save() && $success;
			return $this->updateAssociationTable($obj, $fkObjects, $builder) && $success;
		}
		return $success;
	}

	/**
	 * @return TDbCommandBuilder
	 */
	protected function getAssociationTableCommandBuilder()
	{
		$conn = $this->getContext()->getSourceRecord()->getDbConnection();
		return $this->getAssociationTable()->createCommandBuilder($conn);
	}

	private function hasAssociationData($builder,$data)
	{
		$condition=array();
		$table = $this->getAssociationTable();
		foreach($data as $name=>$value)
			$condition[] = $table->getColumn($name)->getColumnName().' = ?';
		$command = $builder->createCountCommand(implode(' AND ', $condition),array_values($data));
		$result = $this->getCommandBuilder()->onExecuteCommand($command, intval($command->queryScalar()));
		return intval($result) > 0;
	}

	private function addAssociationData($builder,$data)
	{
		$command = $builder->createInsertCommand($data);
		return $this->getCommandBuilder()->onExecuteCommand($command, $command->execute()) > 0;
	}

	private function updateAssociationTable($obj,$fkObjects, $builder)
	{
		$source = $this->getSourceRecordValues($obj);
		$foreignKeys = $this->findForeignKeys($this->getAssociationTable(), $fkObjects[0]);
		$success=true;
		foreach($fkObjects as $fkObject)
		{
			$data = array_merge($source, $this->getForeignObjectValues($foreignKeys,$fkObject));
			if(!$this->hasAssociationData($builder,$data))
				$success = $this->addAssociationData($builder,$data) && $success;
		}
		return $success;
	}

	private function getSourceRecordValues($obj)
	{
		$sourceKeys = $this->findForeignKeys($this->getAssociationTable(), $obj);
		$indexValues = $this->getIndexValues(array_values($sourceKeys), $obj);
		$data = array();
		$i=0;
		foreach($sourceKeys as $name=>$srcKey)
			$data[$name] = $indexValues[0][$i++];
		return $data;
	}

	private function getForeignObjectValues($foreignKeys,$fkObject)
	{
		$data=array();
		foreach($foreignKeys as $name=>$fKey)
			$data[$name] = $fkObject->getColumnValue($fKey);
		return $data;
	}
}
