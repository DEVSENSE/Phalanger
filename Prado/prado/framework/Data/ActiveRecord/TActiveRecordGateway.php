<?php
/**
 * TActiveRecordGateway, TActiveRecordStatementType, TActiveRecordEventParameter classes file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveRecordGateway.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.ActiveRecord
 */

/**
 * TActiveRecordGateway excutes the SQL command queries and returns the data
 * record as arrays (for most finder methods).
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TActiveRecordGateway.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.ActiveRecord
 * @since 3.1
 */
class TActiveRecordGateway extends TComponent
{
	private $_manager;
	private $_tables=array(); //table cache
	private $_meta=array(); //meta data cache.
	private $_commandBuilders=array();
	private $_currentRecord;

	/**
	 * Constant name for specifying optional table name in TActiveRecord.
	 */
	const TABLE_CONST='TABLE';
	/**
	 * Method name for returning optional table name in in TActiveRecord
	 */
	const TABLE_METHOD='table';

	/**
	 * Record gateway constructor.
	 * @param TActiveRecordManager $manager
	 */
	public function __construct(TActiveRecordManager $manager)
	{
		$this->_manager=$manager;
	}

	/**
	 * @return TActiveRecordManager record manager.
	 */
	protected function getManager()
	{
		return $this->_manager;
	}

	/**
	 * Gets the table name from the 'TABLE' constant of the active record
	 * class if defined, otherwise use the class name as table name.
	 * @param TActiveRecord active record instance
	 * @return string table name for the given record class.
	 */
	protected function getRecordTableName(TActiveRecord $record)
	{
		$class = new ReflectionClass($record);
		if($class->hasConstant(self::TABLE_CONST))
		{
			$value = $class->getConstant(self::TABLE_CONST);
			if(empty($value))
				throw new TActiveRecordException('ar_invalid_tablename_property',
					get_class($record),self::TABLE_CONST);
			return $value;
		}
		elseif ($class->hasMethod(self::TABLE_METHOD))
		{
			$value = $record->{self::TABLE_METHOD}();
			if(empty($value))
				throw new TActiveRecordException('ar_invalid_tablename_method',
					get_class($record),self::TABLE_METHOD);
			return $value;
		}
		else
			return strtolower(get_class($record));
	}

	/**
	 * Returns table information, trys the application cache first.
	 * @param TActiveRecord $record
	 * @return TDbTableInfo table information.
	 */
	public function getRecordTableInfo(TActiveRecord $record)
	{
		$tableName = $this->getRecordTableName($record);
		return $this->getTableInfo($record->getDbConnection(), $tableName);
	}

	/**
	 * Returns table information for table in the database connection.
	 * @param TDbConnection database connection
	 * @param string table name
	 * @return TDbTableInfo table details.
	 */
	public function getTableInfo(TDbConnection $connection, $tableName)
	{
		$connStr = $connection->getConnectionString();
		$key = $connStr.$tableName;
		if(!isset($this->_tables[$key]))
		{
			//call this first to ensure that unserializing the cache
			//will find the correct driver dependent classes.
			if(!isset($this->_meta[$connStr]))
			{
				Prado::using('System.Data.Common.TDbMetaData');
				$this->_meta[$connStr] = TDbMetaData::getInstance($connection);
			}

			$tableInfo = null;
			if(($cache=$this->getManager()->getCache())!==null)
				$tableInfo = $cache->get($key);
			if(empty($tableInfo))
			{
				$tableInfo = $this->_meta[$connStr]->getTableInfo($tableName);
				if($cache!==null)
					$cache->set($key, $tableInfo);
			}
			$this->_tables[$key] = $tableInfo;
		}
		return $this->_tables[$key];
	}

	/**
	 * @param TActiveRecord $record
	 * @return TDataGatewayCommand
	 */
	public function getCommand(TActiveRecord $record)
	{
		$conn = $record->getDbConnection();
		$connStr = $conn->getConnectionString();
		$tableInfo = $this->getRecordTableInfo($record);
		if(!isset($this->_commandBuilders[$connStr]))
		{
			$builder = $tableInfo->createCommandBuilder($record->getDbConnection());
			Prado::using('System.Data.DataGateway.TDataGatewayCommand');
			$command = new TDataGatewayCommand($builder);
			$command->OnCreateCommand[] = array($this, 'onCreateCommand');
			$command->OnExecuteCommand[] = array($this, 'onExecuteCommand');
			$this->_commandBuilders[$connStr] = $command;

		}
		$this->_commandBuilders[$connStr]->getBuilder()->setTableInfo($tableInfo);
		$this->_currentRecord=$record;
		return $this->_commandBuilders[$connStr];
	}

	/**
	 * Raised when a command is prepared and parameter binding is completed.
	 * The parameter object is TDataGatewayEventParameter of which the
	 * {@link TDataGatewayEventParameter::getCommand Command} property can be
	 * inspected to obtain the sql query to be executed.
	 * This method also raises the OnCreateCommand event on the ActiveRecord
	 * object calling this gateway.
	 * @param TDataGatewayCommand originator $sender
	 * @param TDataGatewayEventParameter
	 */
	public function onCreateCommand($sender, $param)
	{
		$this->raiseEvent('OnCreateCommand', $this, $param);
		if($this->_currentRecord!==null)
			$this->_currentRecord->onCreateCommand($param);
	}

	/**
	 * Raised when a command is executed and the result from the database was returned.
	 * The parameter object is TDataGatewayResultEventParameter of which the
	 * {@link TDataGatewayEventParameter::getResult Result} property contains
	 * the data return from the database. The data returned can be changed
	 * by setting the {@link TDataGatewayEventParameter::setResult Result} property.
	 * This method also raises the OnCreateCommand event on the ActiveRecord
	 * object calling this gateway.
	 * @param TDataGatewayCommand originator $sender
	 * @param TDataGatewayResultEventParameter
	 */
	public function onExecuteCommand($sender, $param)
	{
		$this->raiseEvent('OnExecuteCommand', $this, $param);
		if($this->_currentRecord!==null)
			$this->_currentRecord->onExecuteCommand($param);
	}

	/**
	 * Returns record data matching the given primary key(s). If the table uses
	 * composite key, specify the name value pairs as an array.
	 * @param TActiveRecord active record instance.
	 * @param array primary name value pairs
	 * @return array record data
	 */
	public function findRecordByPK(TActiveRecord $record,$keys)
	{
		$command = $this->getCommand($record);
		return $command->findByPk($keys);
	}

	/**
	 * Returns records matching the list of given primary keys.
	 * @param TActiveRecord active record instance.
	 * @param array list of primary name value pairs
	 * @return array matching data.
	 */
	public function findRecordsByPks(TActiveRecord $record, $keys)
	{
		return $this->getCommand($record)->findAllByPk($keys);
	}


	/**
	 * Returns record data matching the given critera. If $iterator is true, it will
	 * return multiple rows as TDbDataReader otherwise it returns the <b>first</b> row data.
	 * @param TActiveRecord active record finder instance.
	 * @param TActiveRecordCriteria search criteria.
	 * @param boolean true to return multiple rows as iterator, false returns first row.
	 * @return mixed matching data.
	 */
	public function findRecordsByCriteria(TActiveRecord $record, $criteria, $iterator=false)
	{
		$command = $this->getCommand($record);
		return $iterator ? $command->findAll($criteria) : $command->find($criteria);
	}

	/**
	 * Return record data from sql query.
	 * @param TActiveRecord active record finder instance.
	 * @param TActiveRecordCriteria sql query
	 * @return array result.
	 */
	public function findRecordBySql(TActiveRecord $record, $criteria)
	{
		return $this->getCommand($record)->findBySql($criteria);
	}

	/**
	 * Return record data from sql query.
	 * @param TActiveRecord active record finder instance.
	 * @param TActiveRecordCriteria sql query
	 * @return TDbDataReader result iterator.
	 */
	public function findRecordsBySql(TActiveRecord $record, $criteria)
	{
		return $this->getCommand($record)->findAllBySql($criteria);
	}

	public function findRecordsByIndex(TActiveRecord $record, $criteria, $fields, $values)
	{
		return $this->getCommand($record)->findAllByIndex($criteria,$fields,$values);
	}

	/**
	 * Returns the number of records that match the given criteria.
	 * @param TActiveRecord active record finder instance.
	 * @param TActiveRecordCriteria search criteria
	 * @return int number of records.
	 */
	public function countRecords(TActiveRecord $record, $criteria)
	{
		return $this->getCommand($record)->count($criteria);
	}

	/**
	 * Insert a new record.
	 * @param TActiveRecord new record.
	 * @return int number of rows affected.
	 */
	public function insert(TActiveRecord $record)
	{
		//$this->updateAssociatedRecords($record,true);
		$result = $this->getCommand($record)->insert($this->getInsertValues($record));
		if($result)
			$this->updatePostInsert($record);
		//$this->updateAssociatedRecords($record);
		return $result;
	}

	/**
	 * Sets the last insert ID to the corresponding property of the record if available.
	 * @param TActiveRecord record for insertion
	 */
	protected function updatePostInsert($record)
	{
		$command = $this->getCommand($record);
		$tableInfo = $command->getTableInfo();
		foreach($tableInfo->getColumns() as $name => $column)
		{
			if($column->hasSequence())
				$record->setColumnValue($name,$command->getLastInsertID($column->getSequenceName()));
		}
	}

	/**
	 * @param TActiveRecord record
	 * @return array insert values.
	 */
	protected function getInsertValues(TActiveRecord $record)
	{
		$values=array();
		$tableInfo = $this->getCommand($record)->getTableInfo();
		foreach($tableInfo->getColumns() as $name=>$column)
		{
			if($column->getIsExcluded())
				continue;
			$value = $record->getColumnValue($name);
			if(!$column->getAllowNull() && $value===null && !$column->hasSequence() && ($column->getDefaultValue() === TDbTableColumn::UNDEFINED_VALUE))
			{
				throw new TActiveRecordException(
					'ar_value_must_not_be_null', get_class($record),
					$tableInfo->getTableFullName(), $name);
			}
			if($value!==null)
				$values[$name] = $value;
		}
		return $values;
	}

	/**
	 * Update the record.
	 * @param TActiveRecord dirty record.
	 * @return int number of rows affected.
	 */
	public function update(TActiveRecord $record)
	{
		//$this->updateAssociatedRecords($record,true);
		list($data, $keys) = $this->getUpdateValues($record);
		$result = $this->getCommand($record)->updateByPk($data, $keys);
		//$this->updateAssociatedRecords($record);
		return $result;
	}

	protected function getUpdateValues(TActiveRecord $record)
	{
		$values=array();
		$tableInfo = $this->getCommand($record)->getTableInfo();
		$primary=array();
		foreach($tableInfo->getColumns() as $name=>$column)
		{
			if($column->getIsExcluded())
				continue;
			$value = $record->getColumnValue($name);
			if(!$column->getAllowNull() && $value===null && ($column->getDefaultValue() === TDbTableColumn::UNDEFINED_VALUE))
			{
				throw new TActiveRecordException(
					'ar_value_must_not_be_null', get_class($record),
					$tableInfo->getTableFullName(), $name);
			}
			if($column->getIsPrimaryKey())
				$primary[$name] = $value;
			else
				$values[$name] = $value;
		}
		return array($values,$primary);
	}

	protected function updateAssociatedRecords(TActiveRecord $record,$updateBelongsTo=false)
	{
		$context = new TActiveRecordRelationContext($record);
		return $context->updateAssociatedRecords($updateBelongsTo);
	}

	/**
	 * Delete the record.
	 * @param TActiveRecord record to be deleted.
	 * @return int number of rows affected.
	 */
	public function delete(TActiveRecord $record)
	{
		return $this->getCommand($record)->deleteByPk($this->getPrimaryKeyValues($record));
	}

	protected function getPrimaryKeyValues(TActiveRecord $record)
	{
		$tableInfo = $this->getCommand($record)->getTableInfo();
		$primary=array();
		foreach($tableInfo->getColumns() as $name=>$column)
		{
			if($column->getIsPrimaryKey())
				$primary[$name] = $record->getColumnValue($name);
		}
		return $primary;
	}

	/**
	 * Delete multiple records using primary keys.
	 * @param TActiveRecord finder instance.
	 * @return int number of rows deleted.
	 */
	public function deleteRecordsByPk(TActiveRecord $record, $keys)
	{
		return $this->getCommand($record)->deleteByPk($keys);
	}

	/**
	 * Delete multiple records by criteria.
	 * @param TActiveRecord active record finder instance.
	 * @param TActiveRecordCriteria search criteria
	 * @return int number of records.
	 */
	public function deleteRecordsByCriteria(TActiveRecord $record, $criteria)
	{
		return $this->getCommand($record)->delete($criteria);
	}

	/**
	 * Raise the corresponding command event, insert, update, delete or select.
	 * @param string command type
	 * @param TDbCommand sql command to be executed.
	 * @param TActiveRecord active record
	 * @param TActiveRecordCriteria data for the command.
	 */
	protected function raiseCommandEvent($event,$command,$record,$criteria)
	{
		if(!($criteria instanceof TSqlCriteria))
			$criteria = new TActiveRecordCriteria(null,$criteria);
		$param = new TActiveRecordEventParameter($command,$record,$criteria);
		$manager = $record->getRecordManager();
		$manager->{$event}($param);
		$record->{$event}($param);
	}
}

