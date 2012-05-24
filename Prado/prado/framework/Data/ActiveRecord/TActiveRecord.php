<?php
/**
 * TActiveRecord, TActiveRecordEventParameter, TActiveRecordInvalidFinderResult class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveRecord.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.ActiveRecord
 */

/**
 * Load record manager, criteria and relations.
 */
Prado::using('System.Data.ActiveRecord.TActiveRecordManager');
Prado::using('System.Data.ActiveRecord.TActiveRecordCriteria');
Prado::using('System.Data.ActiveRecord.Relations.TActiveRecordRelationContext');

/**
 * Base class for active records.
 *
 * An active record creates an object that wraps a row in a database table
 * or view, encapsulates the database access, and adds domain logic on that data.
 *
 * Active record objects are stateful, this is main difference between the
 * TActiveRecord implementation and the TTableGateway implementation.
 *
 * The essence of an Active Record is an object model of the
 * domain (e.g. products, items) that incorporates both behavior and
 * data in which the classes match very closely the record structure of an
 * underlying database. Each Active Record is responsible for saving and
 * loading to the database and also for any domain logic that acts on the data.
 *
 * The Active Record provides methods that do the following:
 *  1. Construct an instance of the Active Record from a SQL result set row.
 *  2. Construct a new instance for later insertion into the table.
 *  3. Finder methods to wrap commonly used SQL queries and return Active Record objects.
 *  4. Update the database and insert into it the data in the Active Record.
 *
 * Example:
 * <code>
 * class UserRecord extends TActiveRecord
 * {
 *     const TABLE='users'; //optional table name.
 *
 *     public $username; //corresponds to the fieldname in the table
 *     public $email;
 *
 *     //returns active record finder instance
 *     public static function finder($className=__CLASS__)
 *     {
 *         return parent::finder($className);
 *     }
 * }
 *
 * //create a connection and give it to the ActiveRecord manager.
 * $dsn = 'pgsql:host=localhost;dbname=test';
 * $conn = new TDbConnection($dsn, 'dbuser','dbpass');
 * TActiveRecordManager::getInstance()->setDbConnection($conn);
 *
 * //load the user record with username (primary key) 'admin'.
 * $user = UserRecord::finder()->findByPk('admin');
 * $user->email = 'admin@example.org';
 * $user->save(); //update the 'admin' record.
 * </code>
 *
 * Since v3.1.1, TActiveRecord starts to support column mapping. The physical
 * column names (defined in database) can be mapped to logical column names
 * (defined in active classes as public properties.) To use this feature, declare
 * a static class variable COLUMN_MAPPING like the following:
 * <code>
 * class UserRecord extends TActiveRecord
 * {
 *     const TABLE='users';
 *     public static $COLUMN_MAPPING=array
 *     (
 *         'user_id'=>'username',
 *         'email_address'=>'email',
 *     );
 *     public $username;
 *     public $email;
 * }
 * </code>
 * In the above, the 'users' table consists of 'user_id' and 'email_address' columns,
 * while the UserRecord class declares 'username' and 'email' properties.
 * By using column mapping, we can regularize the naming convention of column names
 * in active record.
 *
 * Since v3.1.2, TActiveRecord enhanced its support to access of foreign objects.
 * By declaring a public static variable RELATIONS like the following, one can access
 * the corresponding foreign objects easily:
 * <code>
 * class UserRecord extends TActiveRecord
 * {
 *     const TABLE='users';
 *     public static $RELATIONS=array
 *     (
 *         'department'=>array(self::BELONGS_TO, 'DepartmentRecord', 'department_id'),
 *         'contacts'=>array(self::HAS_MANY, 'ContactRecord', 'user_id'),
 *     );
 * }
 * </code>
 * In the above, the users table is related with departments table (represented by
 * DepartmentRecord) and contacts table (represented by ContactRecord). Now, given a UserRecord
 * instance $user, one can access its department and contacts simply by: $user->department and
 * $user->contacts. No explicit data fetching is needed. Internally, the foreign objects are
 * fetched in a lazy way, which avoids unnecessary overhead if the foreign objects are not accessed
 * at all.
 *
 * Since v3.1.2, new events OnInsert, OnUpdate and OnDelete are available.
 * The event OnInsert, OnUpdate and OnDelete methods are executed before
 * inserting, updating, and deleting the current record, respectively. You may override
 * these methods; a TActiveRecordChangeEventParameter parameter is passed to these methods.
 * The property {@link TActiveRecordChangeEventParameter::setIsValid IsValid} of the parameter
 * can be set to false to prevent the change action to be executed. This can be used,
 * for example, to validate the record before the action is executed. For example,
 * in the following the password property is hashed before a new record is inserted.
 * <code>
 * class UserRecord extends TActiveRecord
 * {
 *      function OnInsert($param)
 *      {
 *          //parent method should be called to raise the event
 *          parent::OnInsert($param);
 *          $this->nounce = md5(time());
 *          $this->password = md5($this->password.$this->nounce);
 *      }
 * }
 * </code>
 *
 * Since v3.1.3 you can also define a method that returns the table name.
 * <code>
 * class UserRecord extends TActiveRecord
 * {
 *     public function table()
 *     {
 *          return 'users';
 *     }
 *
 * }
 * </code>
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TActiveRecord.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.ActiveRecord
 * @since 3.1
 */
abstract class TActiveRecord extends TComponent
{
	const BELONGS_TO='BELONGS_TO';
	const HAS_ONE='HAS_ONE';
	const HAS_MANY='HAS_MANY';
	const MANY_TO_MANY='MANY_TO_MANY';

	const STATE_NEW=0;
	const STATE_LOADED=1;
	const STATE_DELETED=2;

	/**
	 * @var integer record state: 0 = new, 1 = loaded, 2 = deleted.
	 * @since 3.1.2
	 */
	protected $_recordState=0; // use protected so that serialization is fine

	/**
	 * This static variable defines the column mapping.
	 * The keys are physical column names as defined in database,
	 * and the values are logical column names as defined as public variable/property names
	 * for the corresponding active record class.
	 * @var array column mapping. Keys: physical column names, values: logical column names.
	 * @since 3.1.1
	 */
	public static $COLUMN_MAPPING=array();
	private static $_columnMapping=array();

	/**
	 * This static variable defines the relationships.
	 * The keys are public variable/property names defined in the AR class.
	 * Each value is an array, e.g. array(self::HAS_MANY, 'PlayerRecord').
	 * @var array relationship.
	 * @since 3.1.1
	 */
	public static $RELATIONS=array();
	private static $_relations=array();

	/**
	 * @var TDbConnection database connection object.
	 */
	protected $_connection; // use protected so that serialization is fine


	/**
	 * Defaults to 'null'
	 *
	 * @var TActiveRecordInvalidFinderResult
	 * @since 3.1.5
	 */
	protected $_invalidFinderResult = null; // use protected so that serialization is fine

	/**
	 * Prevent __call() method creating __sleep() when serializing.
	 */
	public function __sleep()
	{
		$this->_connection=null;
		return array_keys(get_object_vars($this));
	}

	/**
	 * Prevent __call() method creating __wakeup() when unserializing.
	 */
	public function __wakeup()
	{
		$this->setupColumnMapping();
		$this->setupRelations();
	}

	/**
	 * Create a new instance of an active record with given $data. The record
	 * can be saved to the database specified by the $connection object.
	 *
	 * @param array optional name value pair record data.
	 * @param TDbConnection optional database connection this object record use.
	 */
	public function __construct($data=array(), $connection=null)
	{
		if($connection!==null)
			$this->setDbConnection($connection);
		$this->setupColumnMapping();
		$this->setupRelations();
		if(!empty($data)) //$data may be an object
			$this->copyFrom($data);
	}

	/**
	 * Magic method for reading properties.
	 * This method is overriden to provide read access to the foreign objects via
	 * the key names declared in the RELATIONS array.
	 * @param string property name
	 * @return mixed property value.
	 * @since 3.1.2
	 */
	public function __get($name)
	{
		if($this->hasRecordRelation($name) && !$this->canGetProperty($name))
		{
			$this->fetchResultsFor($name);
			return $this->$name;
		}
		return parent::__get($name);
	}

	/**
	 * Magic method for writing properties.
	 * This method is overriden to provide write access to the foreign objects via
	 * the key names declared in the RELATIONS array.
	 * @param string property name
	 * @param mixed property value.
	 * @since 3.1.2
	 */
	public function __set($name,$value)
	{
		if($this->hasRecordRelation($name) && !$this->canSetProperty($name))
			$this->$name=$value;
		else
			parent::__set($name,$value);
	}

	/**
	 * @since 3.1.1
	 */
	private function setupColumnMapping()
	{
		$className=get_class($this);
		if(!isset(self::$_columnMapping[$className]))
		{
			$class=new ReflectionClass($className);
			self::$_columnMapping[$className]=$class->getStaticPropertyValue('COLUMN_MAPPING');
		}
	}

	/**
	 * @since 3.1.2
	 */
	private function setupRelations()
	{
		$className=get_class($this);
		if(!isset(self::$_relations[$className]))
		{
			$class=new ReflectionClass($className);
			$relations=array();
			foreach($class->getStaticPropertyValue('RELATIONS') as $key=>$value)
				$relations[strtolower($key)]=array($key,$value);
			self::$_relations[$className]=$relations;
		}
	}

	/**
	 * Copies data from an array or another object.
	 * @throws TActiveRecordException if data is not array or not object.
	 */
	public function copyFrom($data)
	{
		if(is_object($data))
			$data=get_object_vars($data);
		if(!is_array($data))
			throw new TActiveRecordException('ar_data_invalid', get_class($this));
		foreach($data as $name=>$value)
			$this->setColumnValue($name,$value);
	}


	public static function getActiveDbConnection()
	{
		if(($db=self::getRecordManager()->getDbConnection())!==null)
			$db->setActive(true);
		return $db;
	}

	/**
	 * Gets the current Db connection, the connection object is obtained from
	 * the TActiveRecordManager if connection is currently null.
	 * @return TDbConnection current db connection for this object.
	 */
	public function getDbConnection()
	{
		if($this->_connection===null)
			$this->_connection=self::getActiveDbConnection();
		return $this->_connection;
	}

	/**
	 * @param TDbConnection db connection object for this record.
	 */
	public function setDbConnection($connection)
	{
		$this->_connection=$connection;
	}

	/**
	 * @return TDbTableInfo the meta information of the table associated with this AR class.
	 */
	public function getRecordTableInfo()
	{
		return $this->getRecordGateway()->getRecordTableInfo($this);
	}

	/**
	 * Compare two records using their primary key values (all column values if
	 * table does not defined primary keys). The default uses simple == for
	 * comparison of their values. Set $strict=true for identity comparison (===).
	 * @param TActiveRecord another record to compare with.
	 * @param boolean true to perform strict identity comparison
	 * @return boolean true if $record equals, false otherwise.
	 */
	public function equals(TActiveRecord $record, $strict=false)
	{
		if($record===null || get_class($this)!==get_class($record))
			return false;
		$tableInfo = $this->getRecordTableInfo();
		$pks = $tableInfo->getPrimaryKeys();
		$properties = count($pks) > 0 ? $pks : $tableInfo->getColumns()->getKeys();
		$equals=true;
		foreach($properties as $prop)
		{
			if($strict)
				$equals = $equals && $this->getColumnValue($prop) === $record->getColumnValue($prop);
			else
				$equals = $equals && $this->getColumnValue($prop) == $record->getColumnValue($prop);
			if(!$equals)
				return false;
		}
		return $equals;
	}

	/**
	 * Returns the instance of a active record finder for a particular class.
	 * The finder objects are static instances for each ActiveRecord class.
	 * This means that event handlers bound to these finder instances are class wide.
	 * Create a new instance of the ActiveRecord class if you wish to bound the
	 * event handlers to object instance.
	 * @param string active record class name.
	 * @return TActiveRecord active record finder instance.
	 */
	public static function finder($className=__CLASS__)
	{
		static $finders = array();
		if(!isset($finders[$className]))
		{
			$f = Prado::createComponent($className);
			$finders[$className]=$f;
		}
		return $finders[$className];
	}

	/**
	 * Gets the record manager for this object, the default is to call
	 * TActiveRecordManager::getInstance().
	 * @return TActiveRecordManager default active record manager.
	 */
	public static function getRecordManager()
	{
		return TActiveRecordManager::getInstance();
	}

	/**
	 * @return TActiveRecordGateway record table gateway.
	 */
	public function getRecordGateway()
	{
		return TActiveRecordManager::getInstance()->getRecordGateway();
	}

	/**
	 * Saves the current record to the database, insert or update is automatically determined.
	 * @return boolean true if record was saved successfully, false otherwise.
	 */
	public function save()
	{
		$gateway = $this->getRecordGateway();
		$param = new TActiveRecordChangeEventParameter();
		if($this->_recordState===self::STATE_NEW)
		{
			$this->onInsert($param);
			if($param->getIsValid() && $gateway->insert($this))
			{
				$this->_recordState = self::STATE_LOADED;
				return true;
			}
		}
		else if($this->_recordState===self::STATE_LOADED)
		{
			$this->onUpdate($param);
			if($param->getIsValid() && $gateway->update($this))
				return true;
		}
		else
			throw new TActiveRecordException('ar_save_invalid', get_class($this));

		return false;
	}

	/**
	 * Deletes the current record from the database. Once deleted, this object
	 * can not be saved again in the same instance.
	 * @return boolean true if the record was deleted successfully, false otherwise.
	 */
	public function delete()
	{
		if($this->_recordState===self::STATE_LOADED)
		{
			$gateway = $this->getRecordGateway();
			$param = new TActiveRecordChangeEventParameter();
			$this->onDelete($param);
			if($param->getIsValid() && $gateway->delete($this))
			{
				$this->_recordState=self::STATE_DELETED;
				return true;
			}
		}
		else
			throw new TActiveRecordException('ar_delete_invalid', get_class($this));

		return false;
	}

	/**
	 * Delete records by primary key. Usage:
	 *
	 * <code>
	 * $finder->deleteByPk($primaryKey); //delete 1 record
	 * $finder->deleteByPk($key1,$key2,...); //delete multiple records
	 * $finder->deleteByPk(array($key1,$key2,...)); //delete multiple records
	 * </code>
	 *
	 * For composite primary keys (determined from the table definitions):
	 * <code>
	 * $finder->deleteByPk(array($key1,$key2)); //delete 1 record
	 *
	 * //delete multiple records
	 * $finder->deleteByPk(array($key1,$key2), array($key3,$key4),...);
	 *
	 * //delete multiple records
	 * $finder->deleteByPk(array( array($key1,$key2), array($key3,$key4), .. ));
	 * </code>
	 *
	 * @param mixed primary key values.
	 * @return int number of records deleted.
	 */
	public function deleteByPk($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		return $this->getRecordGateway()->deleteRecordsByPk($this,(array)$keys);
	}

	/**
	 * Alias for deleteByPk()
	 */
	public function deleteAllByPks($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		return $this->deleteByPk($keys);
	}
	/**
	 * Delete multiple records using a criteria.
	 * @param string|TActiveRecordCriteria SQL condition or criteria object.
	 * @param mixed parameter values.
	 * @return int number of records deleted.
	 */
	public function deleteAll($criteria=null, $parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getRecordCriteria($criteria,$parameters, $args);
		return $this->getRecordGateway()->deleteRecordsByCriteria($this, $criteria);
	}

	/**
	 * Populates a new record with the query result.
	 * This is a wrapper of {@link createRecord}.
	 * @param array name value pair of record data
	 * @return TActiveRecord object record, null if data is empty.
	 */
	protected function populateObject($data)
	{
		return self::createRecord(get_class($this), $data);
	}

	/**
	 * @param TDbDataReader data reader
	 * @return array the AR objects populated by the query result
	 * @since 3.1.2
	 */
	protected function populateObjects($reader)
	{
		$result=array();
		foreach($reader as $data)
			$result[] = $this->populateObject($data);
		return $result;
	}

	/**
	 * Create an AR instance specified by the AR class name and initial data.
	 * If the initial data is empty, the AR object will not be created and null will be returned.
	 * (You should use the "new" operator to create the AR instance in that case.)
	 * @param string the AR class name
	 * @param array initial data to be populated into the AR object.
	 * @return TActiveRecord the initialized AR object. Null if the initial data is empty.
	 * @since 3.1.2
	 */
	public static function createRecord($type, $data)
	{
		if(empty($data))
			return null;
		$record=new $type($data);
		$record->_recordState=self::STATE_LOADED;
		return $record;
	}

	/**
	 * Find one single record that matches the criteria.
	 *
	 * Usage:
	 * <code>
	 * $finder->find('username = :name AND password = :pass',
	 * 					array(':name'=>$name, ':pass'=>$pass));
	 * $finder->find('username = ? AND password = ?', array($name, $pass));
	 * $finder->find('username = ? AND password = ?', $name, $pass);
	 * //$criteria is of TActiveRecordCriteria
	 * $finder->find($criteria); //the 2nd parameter for find() is ignored.
	 * </code>
	 *
	 * @param string|TActiveRecordCriteria SQL condition or criteria object.
	 * @param mixed parameter values.
	 * @return TActiveRecord matching record object. Null if no result is found.
	 */
	public function find($criteria,$parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getRecordCriteria($criteria,$parameters, $args);
		$criteria->setLimit(1);
		$data = $this->getRecordGateway()->findRecordsByCriteria($this,$criteria);
		return $this->populateObject($data);
	}

	/**
	 * Same as find() but returns an array of objects.
	 *
	 * @param string|TActiveRecordCriteria SQL condition or criteria object.
	 * @param mixed parameter values.
	 * @return array matching record objects. Empty array if no result is found.
	 */
	public function findAll($criteria=null,$parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		if($criteria!==null)
			$criteria = $this->getRecordCriteria($criteria,$parameters, $args);
		$result = $this->getRecordGateway()->findRecordsByCriteria($this,$criteria,true);
		return $this->populateObjects($result);
	}

	/**
	 * Find one record using only the primary key or composite primary keys. Usage:
	 *
	 * <code>
	 * $finder->findByPk($primaryKey);
	 * $finder->findByPk($key1, $key2, ...);
	 * $finder->findByPk(array($key1,$key2,...));
	 * </code>
	 *
	 * @param mixed primary keys
	 * @return TActiveRecord. Null if no result is found.
	 */
	public function findByPk($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		$data = $this->getRecordGateway()->findRecordByPK($this,$keys);
		return $this->populateObject($data);
	}

	/**
	 * Find multiple records matching a list of primary or composite keys.
	 *
	 * For scalar primary keys:
	 * <code>
	 * $finder->findAllByPk($key1, $key2, ...);
	 * $finder->findAllByPk(array($key1, $key2, ...));
	 * </code>
	 *
	 * For composite keys:
	 * <code>
	 * $finder->findAllByPk(array($key1, $key2), array($key3, $key4), ...);
	 * $finder->findAllByPk(array(array($key1, $key2), array($key3, $key4), ...));
	 * </code>
	 * @param mixed primary keys
	 * @return array matching ActiveRecords. Empty array is returned if no result is found.
	 */
	public function findAllByPks($keys)
	{
		if(func_num_args() > 1)
			$keys = func_get_args();
		$result = $this->getRecordGateway()->findRecordsByPks($this,(array)$keys);
		return $this->populateObjects($result);
	}

	/**
	 * Find records using full SQL, returns corresponding record object.
	 * The names of the column retrieved must be defined in your Active Record
	 * class.
	 * @param string select SQL
	 * @param array $parameters
	 * @return TActiveRecord, null if no result is returned.
	 */
	public function findBySql($sql,$parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getRecordCriteria($sql,$parameters, $args);
		$criteria->setLimit(1);
		$data = $this->getRecordGateway()->findRecordBySql($this,$criteria);
		return $this->populateObject($data);
	}

	/**
	 * Find records using full SQL, returns corresponding record object.
	 * The names of the column retrieved must be defined in your Active Record
	 * class.
	 * @param string select SQL
	 * @param array $parameters
	 * @return array matching active records. Empty array is returned if no result is found.
	 */
	public function findAllBySql($sql,$parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		$criteria = $this->getRecordCriteria($sql,$parameters, $args);
		$result = $this->getRecordGateway()->findRecordsBySql($this,$criteria);
		return $this->populateObjects($result);
	}

	/**
	 * Fetches records using the sql clause "(fields) IN (values)", where
	 * fields is an array of column names and values is an array of values that
	 * the columns must have.
	 *
	 * This method is to be used by the relationship handler.
	 *
	 * @param TActiveRecordCriteria additional criteria
	 * @param array field names to match with "(fields) IN (values)" sql clause.
	 * @param array matching field values.
	 * @return array matching active records. Empty array is returned if no result is found.
	 */
	public function findAllByIndex($criteria,$fields,$values)
	{
		$result = $this->getRecordGateway()->findRecordsByIndex($this,$criteria,$fields,$values);
		return $this->populateObjects($result);
	}

	/**
	 * Find the number of records.
	 * @param string|TActiveRecordCriteria SQL condition or criteria object.
	 * @param mixed parameter values.
	 * @return int number of records.
	 */
	public function count($criteria=null,$parameters=array())
	{
		$args = func_num_args() > 1 ? array_slice(func_get_args(),1) : null;
		if($criteria!==null)
			$criteria = $this->getRecordCriteria($criteria,$parameters, $args);
		return $this->getRecordGateway()->countRecords($this,$criteria);
	}

	/**
	 * Returns the active record relationship handler for $RELATION with key
	 * value equal to the $property value.
	 * @param string relationship/property name corresponding to keys in $RELATION array.
	 * @param array method call arguments.
	 * @return TActiveRecordRelation, null if the context or the handler doesn't exist
	 */
	protected function getRelationHandler($name,$args=array())
	{
		if(($context=$this->createRelationContext($name)) !== null)
		{
			$criteria = $this->getRecordCriteria(count($args)>0 ? $args[0] : null, array_slice($args,1));
			return $context->getRelationHandler($criteria);
		}
		else
			return null;
	}

	/**
	 * Gets a static copy of the relationship context for given property (a key
	 * in $RELATIONS), returns null if invalid relationship. Keeps a null
	 * reference to all invalid relations called.
	 * @param string relationship/property name corresponding to keys in $RELATION array.
	 * @return TActiveRecordRelationContext object containing information on
	 * the active record relationships for given property, null if invalid relationship
	 * @since 3.1.2
	 */
	protected function createRelationContext($name)
	{
		if(($definition=$this->getRecordRelation($name))!==null)
		{
			list($property, $relation) = $definition;
			return new TActiveRecordRelationContext($this,$property,$relation);
		}
		else
			return null;
	}

	/**
	 * Tries to load the relationship results for the given property. The $property
	 * value should correspond to an entry key in the $RELATION array.
	 * This method can be used to lazy load relationships.
	 * <code>
	 * class TeamRecord extends TActiveRecord
	 * {
	 *     ...
	 *
	 *     private $_players;
	 *     public static $RELATION=array
	 *     (
	 *         'players' => array(self::HAS_MANY, 'PlayerRecord'),
	 *     );
	 *
	 *     public function setPlayers($array)
	 *     {
	 *         $this->_players=$array;
	 *     }
	 *
	 *     public function getPlayers()
	 *     {
	 *         if($this->_players===null)
	 *             $this->fetchResultsFor('players');
	 *         return $this->_players;
	 *     }
	 * }
	 * Usage example:
	 * $team = TeamRecord::finder()->findByPk(1);
	 * var_dump($team->players); //uses lazy load to fetch 'players' relation
	 * </code>
	 * @param string relationship/property name corresponding to keys in $RELATION array.
	 * @return boolean true if relationship exists, false otherwise.
	 * @since 3.1.2
	 */
	protected function fetchResultsFor($property)
	{
		if( ($context=$this->createRelationContext($property)) !== null)
			return $context->getRelationHandler()->fetchResultsInto($this);
		else
			return false;
	}

	/**
	 * Dynamic find method using parts of method name as search criteria.
	 * Method name starting with "findBy" only returns 1 record.
	 * Method name starting with "findAllBy" returns 0 or more records.
	 * Method name starting with "deleteBy" deletes records by the trail criteria.
	 * The condition is taken as part of the method name after "findBy", "findAllBy"
	 * or "deleteBy".
	 *
	 * The following are equivalent:
	 * <code>
	 * $finder->findByName($name)
	 * $finder->find('Name = ?', $name);
	 * </code>
	 * <code>
	 * $finder->findByUsernameAndPassword($name,$pass); // OR may be used
	 * $finder->findBy_Username_And_Password($name,$pass); // _OR_ may be used
	 * $finder->find('Username = ? AND Password = ?', $name, $pass);
	 * </code>
	 * <code>
	 * $finder->findAllByAge($age);
	 * $finder->findAll('Age = ?', $age);
	 * </code>
	 * <code>
	 * $finder->deleteAll('Name = ?', $name);
	 * $finder->deleteByName($name);
	 * </code>
	 * @return mixed single record if method name starts with "findBy", 0 or more records
	 * if method name starts with "findAllBy"
	 */
	public function __call($method,$args)
	{
		$delete =false;
		if(strncasecmp($method,'with',4)===0)
		{
			$property= $method[4]==='_' ? substr($method,5) : substr($method,4);
			return $this->getRelationHandler($property, $args);
		}
		else if($findOne=strncasecmp($method,'findby',6)===0)
			$condition = $method[6]==='_' ? substr($method,7) : substr($method,6);
		else if(strncasecmp($method,'findallby',9)===0)
			$condition = $method[9]==='_' ? substr($method,10) : substr($method,9);
		else if($delete=strncasecmp($method,'deleteby',8)===0)
			$condition = $method[8]==='_' ? substr($method,9) : substr($method,8);
		else if($delete=strncasecmp($method,'deleteallby',11)===0)
			$condition = $method[11]==='_' ? substr($method,12) : substr($method,11);
		else
		{
			if($this->getInvalidFinderResult() == TActiveRecordInvalidFinderResult::Exception)
				throw new TActiveRecordException('ar_invalid_finder_method',$method);
			else
				return null;
		}

		$criteria = $this->getRecordGateway()->getCommand($this)->createCriteriaFromString($method, $condition, $args);
		if($delete)
			return $this->deleteAll($criteria);
		else
			return $findOne ? $this->find($criteria) : $this->findAll($criteria);
	}

	/**
	 * @return TActiveRecordInvalidFinderResult Defaults to '{@link TActiveRecordInvalidFinderResult::Null Null}'.
	 * @see TActiveRecordManager::getInvalidFinderResult
	 * @since 3.1.5
	 */
	public function getInvalidFinderResult()
	{
		if($this->_invalidFinderResult !== null)
			return $this->_invalidFinderResult;

		return self::getRecordManager()->getInvalidFinderResult();
	}

	/**
	 * Define the way an active record finder react if an invalid magic-finder invoked
	 *
	 * @param TActiveRecordInvalidFinderResult|null
	 * @see TActiveRecordManager::setInvalidFinderResult
	 * @since 3.1.5
	 */
	public function setInvalidFinderResult($value)
	{
		if($value === null)
			$this->_invalidFinderResult = null;
		else
			$this->_invalidFinderResult = TPropertyValue::ensureEnum($value, 'TActiveRecordInvalidFinderResult');
	}

	/**
	 * Create a new TSqlCriteria object from a string $criteria. The $args
	 * are additional parameters and are used in place of the $parameters
	 * if $parameters is not an array and $args is an arrary.
	 * @param string|TSqlCriteria sql criteria
	 * @param mixed parameters passed by the user.
	 * @param array additional parameters obtained from function_get_args().
	 * @return TSqlCriteria criteria object.
	 */
	protected function getRecordCriteria($criteria, $parameters, $args=array())
	{
		if(is_string($criteria))
		{
			$useArgs = !is_array($parameters) && is_array($args);
			return new TActiveRecordCriteria($criteria,$useArgs ? $args : $parameters);
		}
		else if($criteria instanceof TSqlCriteria)
			return $criteria;
		else
			return new TActiveRecordCriteria();
			//throw new TActiveRecordException('ar_invalid_criteria');
	}

	/**
	 * Raised when a command is prepared and parameter binding is completed.
	 * The parameter object is TDataGatewayEventParameter of which the
	 * {@link TDataGatewayEventParameter::getCommand Command} property can be
	 * inspected to obtain the sql query to be executed.
	 *
	 * Note well that the finder objects obtained from ActiveRecord::finder()
	 * method are static objects. This means that the event handlers are
	 * bound to a static finder object and not to each distinct active record object.
	 * @param TDataGatewayEventParameter
	 */
	public function onCreateCommand($param)
	{
		$this->raiseEvent('OnCreateCommand', $this, $param);
	}

	/**
	 * Raised when a command is executed and the result from the database was returned.
	 * The parameter object is TDataGatewayResultEventParameter of which the
	 * {@link TDataGatewayEventParameter::getResult Result} property contains
	 * the data return from the database. The data returned can be changed
	 * by setting the {@link TDataGatewayEventParameter::setResult Result} property.
	 *
	 * Note well that the finder objects obtained from ActiveRecord::finder()
	 * method are static objects. This means that the event handlers are
	 * bound to a static finder object and not to each distinct active record object.
	 * @param TDataGatewayResultEventParameter
	 */
	public function onExecuteCommand($param)
	{
		$this->raiseEvent('OnExecuteCommand', $this, $param);
	}

	/**
	 * Raised before the record attempt to insert its data into the database.
	 * To prevent the insert operation, set the TActiveRecordChangeEventParameter::IsValid parameter to false.
	 * @param TActiveRecordChangeEventParameter event parameter to be passed to the event handlers
	 */
	public function onInsert($param)
	{
		$this->raiseEvent('OnInsert', $this, $param);
	}

	/**
	 * Raised before the record attempt to delete its data from the database.
	 * To prevent the delete operation, set the TActiveRecordChangeEventParameter::IsValid parameter to false.
	 * @param TActiveRecordChangeEventParameter event parameter to be passed to the event handlers
	 */
	public function onDelete($param)
	{
		$this->raiseEvent('OnDelete', $this, $param);
	}

	/**
	 * Raised before the record attempt to update its data in the database.
	 * To prevent the update operation, set the TActiveRecordChangeEventParameter::IsValid parameter to false.
	 * @param TActiveRecordChangeEventParameter event parameter to be passed to the event handlers
	 */
	public function onUpdate($param)
	{
		$this->raiseEvent('OnUpdate', $this, $param);
	}

	/**
	 * Retrieves the column value according to column name.
	 * This method is used internally.
	 * @param string the column name (as defined in database schema)
	 * @return mixed the corresponding column value
	 * @since 3.1.1
	 */
	public function getColumnValue($columnName)
	{
		$className=get_class($this);
		if(isset(self::$_columnMapping[$className][$columnName]))
			$columnName=self::$_columnMapping[$className][$columnName];
		return $this->$columnName;
	}

	/**
	 * Sets the column value according to column name.
	 * This method is used internally.
	 * @param string the column name (as defined in database schema)
	 * @param mixed the corresponding column value
	 * @since 3.1.1
	 */
	public function setColumnValue($columnName,$value)
	{
		$className=get_class($this);
		if(isset(self::$_columnMapping[$className][$columnName]))
			$columnName=self::$_columnMapping[$className][$columnName];
		$this->$columnName=$value;
	}

	/**
	 * @param string relation property name
	 * @return array relation definition for the specified property
	 * @since 3.1.2
	 */
	public function getRecordRelation($property)
	{
		$className=get_class($this);
		$property=strtolower($property);
		return isset(self::$_relations[$className][$property])?self::$_relations[$className][$property]:null;
	}

	/**
	 * @return array all relation definitions declared in the AR class
	 * @since 3.1.2
	 */
	public function getRecordRelations()
	{
		return self::$_relations[get_class($this)];
	}

	/**
	 * @param string AR property name
	 * @return boolean whether a relation is declared for the specified AR property
	 * @since 3.1.2
	 */
	public function hasRecordRelation($property)
	{
		return isset(self::$_relations[get_class($this)][strtolower($property)]);
	}
}

/**
 * TActiveRecordChangeEventParameter class
 *
 * TActiveRecordChangeEventParameter encapsulates the parameter data for
 * ActiveRecord change commit events that are broadcasted. The following change events
 * may be raise: {@link TActiveRecord::OnInsert}, {@link TActiveRecord::OnUpdate} and
 * {@link TActiveRecord::OnDelete}. The {@link setIsValid IsValid} parameter can
 * be set to false to prevent the requested change event to be performed.
 *
 * @author Wei Zhuo<weizhuo@gmail.com>
 * @version $Id: TActiveRecord.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.ActiveRecord
 * @since 3.1.2
 */
class TActiveRecordChangeEventParameter extends TEventParameter
{
	private $_isValid=true;

	/**
	 * @return boolean whether the event should be performed.
	 */
	public function getIsValid()
	{
		return $this->_isValid;
	}

	/**
	 * @param boolean set to false to prevent the event.
	 */
	public function setIsValid($value)
	{
		$this->_isValid = TPropertyValue::ensureBoolean($value);
	}
}

/**
 * TActiveRecordInvalidFinderResult class.
 * TActiveRecordInvalidFinderResult defines the enumerable type for possible results
 * if an invalid {@link TActiveRecord::__call magic-finder} invoked.
 *
 * The following enumerable values are defined:
 * - Null: return null (default)
 * - Exception: throws a TActiveRecordException
 *
 * @author Yves Berkholz <godzilla80@gmx.net>
 * @version $Id: TActiveRecord.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.ActiveRecord
 * @see TActiveRecordManager::setInvalidFinderResult
 * @see TActiveRecordConfig::setInvalidFinderResult
 * @see TActiveRecord::setInvalidFinderResult
 * @since 3.1.5
 */
class TActiveRecordInvalidFinderResult extends TEnumerable
{
	const Null = 'Null';
	const Exception = 'Exception';
}
