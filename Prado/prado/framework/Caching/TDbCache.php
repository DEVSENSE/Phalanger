<?php
/**
 * TDbCache class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbCache.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Caching
 */

Prado::using('System.Data.TDbConnection');

/**
 * TDbCache class
 *
 * TDbCache implements a cache application module by storing cached data in a database.
 *
 * TDbCache relies on {@link http://www.php.net/manual/en/ref.pdo.php PDO} to retrieve
 * data from databases. In order to use TDbCache, you need to enable the PDO extension
 * as well as the corresponding PDO DB driver. For example, to use SQLite database
 * to store cached data, you need both php_pdo and php_pdo_sqlite extensions.
 *
 * By default, TDbCache creates and uses an SQLite database under the application
 * runtime directory. You may change this default setting by specifying the following
 * properties:
 * - {@link setConnectionID ConnectionID} or
 * - {@link setConnectionString ConnectionString}, {@link setUsername Username} and {@link setPassword Pasword}.
 *
 * The cached data is stored in a table in the specified database.
 * By default, the name of the table is called 'pradocache'. If the table does not
 * exist in the database, it will be automatically created with the following structure:
 * <code>
 * CREATE TABLE pradocache (itemkey CHAR(128), value BLOB, expire INT)
 * CREATE INDEX IX_itemkey ON pradocache (itemkey)
 * CREATE INDEX IX_expire ON pradocache (expire)
 * </code>
 *
 * Note, some DBMS might not support BLOB type. In this case, replace 'BLOB' with a suitable
 * binary data type (e.g. LONGBLOB in MySQL, BYTEA in PostgreSQL.)
 *
 * Important: Make sure that the indices are non-unique!
 *
 * If you want to change the cache table name, or if you want to create the table by yourself,
 * you may set {@link setCacheTableName CacheTableName} and {@link setAutoCreateCacheTable AutoCreateCacheTableName} properties.
 *
 * {@link setFlushInterval FlushInterval} control how often expired items will be removed from cache.
 * If you prefer to remove expired items manualy e.g. via cronjob you can disable automatic deletion by setting FlushInterval to '0'.
 *
 * The following basic cache operations are implemented:
 * - {@link get} : retrieve the value with a key (if any) from cache
 * - {@link set} : store the value with a key into cache
 * - {@link add} : store the value only if cache does not have this key
 * - {@link delete} : delete the value with the specified key from cache
 * - {@link flush} : delete all values from cache
 *
 * Each value is associated with an expiration time. The {@link get} operation
 * ensures that any expired value will not be returned. The expiration time by
 * the number of seconds. A expiration time 0 represents never expire.
 *
 * By definition, cache does not ensure the existence of a value
 * even if it never expires. Cache is not meant to be an persistent storage.
 *
 * Do not use the same database file for multiple applications using TDbCache.
 * Also note, cache is shared by all user sessions of an application.
 *
 * Some usage examples of TDbCache are as follows,
 * <code>
 * $cache=new TDbCache;  // TDbCache may also be loaded as a Prado application module
 * $cache->init(null);
 * $cache->add('object',$object);
 * $object2=$cache->get('object');
 * </code>
 *
 * If loaded, TDbCache will register itself with {@link TApplication} as the
 * cache module. It can be accessed via {@link TApplication::getCache()}.
 *
 * TDbCache may be configured in application configuration file as follows
 * <code>
 * <module id="cache" class="System.Caching.TDbCache" />
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDbCache.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Caching
 * @since 3.1.0
 */
class TDbCache extends TCache
{
	/**
	 * @var string the ID of TDataSourceConfig module
	 */
	private $_connID='';
	/**
	 * @var TDbConnection the DB connection instance
	 */
	private $_db;
	/**
	 * @var string name of the DB cache table
	 */
	private $_cacheTable='pradocache';
	/**
	 * @var integer Interval expired items will be removed from cache
	 */
	private $_flushInterval=60;
	/**
	 * @var boolean
	 */
	private $_cacheInitialized = false;
	/**
	 * @var boolean
	 */
	private $_createCheck= false;
	/**
	 * @var boolean whether the cache DB table should be created automatically
	 */
	private $_autoCreate=true;
	private $_username='';
	private $_password='';
	private $_connectionString='';

	/**
	 * Destructor.
	 * Disconnect the db connection.
	 */
	public function __destruct()
	{
		if($this->_db!==null)
			$this->_db->setActive(false);
	}

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface.
	 * attach {@link doInitializeCache} to TApplication.OnLoadStateComplete event
	 * attach {@link doFlushCacheExpired} to TApplication.OnSaveState event
	 *
	 * @param TXmlElement configuration for this module, can be null
	 */
	public function init($config)
	{
		$this -> getApplication() -> attachEventHandler('OnLoadStateComplete', array($this, 'doInitializeCache'));
		$this -> getApplication() -> attachEventHandler('OnSaveState', array($this, 'doFlushCacheExpired'));
		parent::init($config);
	}

	/**
	 * Event listener for TApplication.OnSaveState
	 * @return void
	 * @since 3.1.5
	 * @see flushCacheExpired
	 */
	public function doFlushCacheExpired()
	{
		$this->flushCacheExpired(false);
	}

	/**
	 * Event listener for TApplication.OnLoadStateComplete
	 *
	 * @return void
	 * @since 3.1.5
	 * @see initializeCache
	 */
	public function doInitializeCache()
	{
		$this->initializeCache();
	}

	/**
	 * Initialize TDbCache
	 *
	 * If {@link setAutoCreateCacheTable AutoCreateCacheTableName} is 'true' check existence of cache table
	 * and create table if does not exist.
	 *
	 * @param boolean Force override global state check
	 * @return void
	 * @throws TConfigurationException if any error happens during creating database or cache table.
	 * @since 3.1.5
	 */
	protected function initializeCache($force=false)
	{
		if($this->_cacheInitialized && !$force) return;
		$db=$this->getDbConnection();
		try
		{
			$key = 'TDbCache:' . $this->_cacheTable . ':created';
			if($force)
				$this -> _createCheck = false;
			else
				$this -> _createCheck = $this -> getApplication() -> getGlobalState($key, 0);

			if($this->_autoCreate && !$this -> _createCheck) {

				Prado::trace(($force ? 'Force initializing: ' : 'Initializing: ') . $this -> id . ', ' . $this->_cacheTable, 'System.Caching.TDbCache');

				$sql='SELECT 1 FROM '.$this->_cacheTable.' WHERE 0=1';
				$db->createCommand($sql)->queryScalar();

				$this -> _createCheck = true;
				$this -> getApplication() -> setGlobalState($key, time());
			}
		}
		catch(Exception $e)
		{
			// DB table not exists
			if($this->_autoCreate)
			{
				Prado::trace('Autocreate: ' . $this->_cacheTable, 'System.Caching.TDbCache');

				$driver=$db->getDriverName();
				if($driver==='mysql')
					$blob='LONGBLOB';
				else if($driver==='pgsql')
					$blob='BYTEA';
				else
					$blob='BLOB';

				$sql='CREATE TABLE '.$this->_cacheTable." (itemkey CHAR(128), value $blob, expire INT)";
				$db->createCommand($sql)->execute();

				$sql='CREATE INDEX IX_itemkey ON ' . $this->_cacheTable . ' (itemkey)';
				$db->createCommand($sql)->execute();

				$sql='CREATE INDEX IX_expire ON ' . $this->_cacheTable . ' (expire)';
				$db->createCommand($sql)->execute();

				$this -> _createCheck = true;
				$this -> getApplication() -> setGlobalState($key, time());
			}
			else
				throw new TConfigurationException('db_cachetable_inexistent',$this->_cacheTable);
		}
		$this->_cacheInitialized = true;
	}

	/**
	 * Flush expired values from cache depending on {@link setFlushInterval FlushInterval}
	 * @param boolean override {@link setFlushInterval FlushInterval} and force deletion of expired items
	 * @return void
	 * @since 3.1.5
	 */
	public function flushCacheExpired($force=false)
	{
		$interval = $this -> getFlushInterval();
		if(!$force && $interval === 0) return;

		$key	= 'TDbCache:' . $this->_cacheTable . ':flushed';
		$now	= time();
		$next	= $interval + (integer)$this -> getApplication() -> getGlobalState($key, 0);

		if($force || $next <= $now)
		{
			if(!$this->_cacheInitialized) $this->initializeCache();
			Prado::trace(($force ? 'Force flush of expired items: ' : 'Flush expired items: ') . $this -> id . ', ' . $this->_cacheTable, 'System.Caching.TDbCache');
			$sql='DELETE FROM '.$this->_cacheTable.' WHERE expire<>0 AND expire<'.$now;
			$this->getDbConnection()->createCommand($sql)->execute();
			$this -> getApplication() -> setGlobalState($key, $now);
		}
	}

	/**
	 * @return integer Interval in sec expired items will be removed from cache. Default to 60
	 * @since 3.1.5
	 */
	public function getFlushInterval()
	{
		return $this->_flushInterval;
	}

	/**
	 * Sets interval expired items will be removed from cache
	 *
	 * To disable automatic deletion of expired items,
	 * e.g. for external flushing via cron you can set value to '0'
	 *
	 * @param integer Interval in sec
	 * @since 3.1.5
	 */
	public function setFlushInterval($value)
	{
		$this->_flushInterval = (integer) $value;
	}

	/**
	 * Creates the DB connection.
	 * @param string the module ID for TDataSourceConfig
	 * @return TDbConnection the created DB connection
	 * @throws TConfigurationException if module ID is invalid or empty
	 */
	protected function createDbConnection()
	{
		if($this->_connID!=='')
		{
			$config=$this->getApplication()->getModule($this->_connID);
			if($config instanceof TDataSourceConfig)
				return $config->getDbConnection();
			else
				throw new TConfigurationException('dbcache_connectionid_invalid',$this->_connID);
		}
		else
		{
			$db=new TDbConnection;
			if($this->_connectionString!=='')
			{
				$db->setConnectionString($this->_connectionString);
				if($this->_username!=='')
					$db->setUsername($this->_username);
				if($this->_password!=='')
					$db->setPassword($this->_password);
			}
			else
			{
				// default to SQLite3 database
				$dbFile=$this->getApplication()->getRuntimePath().'/sqlite3.cache';
				$db->setConnectionString('sqlite:'.$dbFile);
			}
			return $db;
		}
	}

	/**
	 * @return TDbConnection the DB connection instance
	 */
	public function getDbConnection()
	{
		if($this->_db===null)
			$this->_db=$this->createDbConnection();

		$this->_db->setActive(true);
		return $this->_db;
	}

	/**
	 * @return string the ID of a {@link TDataSourceConfig} module. Defaults to empty string, meaning not set.
	 * @since 3.1.1
	 */
	public function getConnectionID()
	{
		return $this->_connID;
	}

	/**
	 * Sets the ID of a TDataSourceConfig module.
	 * The datasource module will be used to establish the DB connection for this cache module.
	 * The database connection can also be specified via {@link setConnectionString ConnectionString}.
	 * When both ConnectionID and ConnectionString are specified, the former takes precedence.
	 * @param string ID of the {@link TDataSourceConfig} module
	 * @since 3.1.1
	 */
	public function setConnectionID($value)
	{
		$this->_connID=$value;
	}

	/**
	 * @return string The Data Source Name, or DSN, contains the information required to connect to the database.
	 */
	public function getConnectionString()
	{
		return $this->_connectionString;
	}

	/**
	 * @param string The Data Source Name, or DSN, contains the information required to connect to the database.
	 * @see http://www.php.net/manual/en/function.pdo-construct.php
	 */
	public function setConnectionString($value)
	{
		$this->_connectionString=$value;
	}

	/**
	 * @return string the username for establishing DB connection. Defaults to empty string.
	 */
	public function getUsername()
	{
		return $this->_username;
	}

	/**
	 * @param string the username for establishing DB connection
	 */
	public function setUsername($value)
	{
		$this->_username=$value;
	}

	/**
	 * @return string the password for establishing DB connection. Defaults to empty string.
	 */
	public function getPassword()
	{
		return $this->_password;
	}

	/**
	 * @param string the password for establishing DB connection
	 */
	public function setPassword($value)
	{
		$this->_password=$value;
	}

	/**
	 * @return string the name of the DB table to store cache content. Defaults to 'pradocache'.
	 * @see setAutoCreateCacheTable
	 */
	public function getCacheTableName()
	{
		return $this->_cacheTable;
	}

	/**
	 * Sets the name of the DB table to store cache content.
	 * Note, if {@link setAutoCreateCacheTable AutoCreateCacheTable} is false
	 * and you want to create the DB table manually by yourself,
	 * you need to make sure the DB table is of the following structure:
	 * <code>
	 * CREATE TABLE pradocache (itemkey CHAR(128), value BLOB, expire INT)
	 * CREATE INDEX IX_itemkey ON pradocache (itemkey)
	 * CREATE INDEX IX_expire ON pradocache (expire)
	 * </code>
	 *
	 * Note, some DBMS might not support BLOB type. In this case, replace 'BLOB' with a suitable
	 * binary data type (e.g. LONGBLOB in MySQL, BYTEA in PostgreSQL.)
	 *
	 * Important: Make sure that the indices are non-unique!
	 *
	 * @param string the name of the DB table to store cache content
	 * @see setAutoCreateCacheTable
	 */
	public function setCacheTableName($value)
	{
		$this->_cacheTable=$value;
	}

	/**
	 * @return boolean whether the cache DB table should be automatically created if not exists. Defaults to true.
	 * @see setAutoCreateCacheTable
	 */
	public function getAutoCreateCacheTable()
	{
		return $this->_autoCreate;
	}

	/**
	 * @param boolean whether the cache DB table should be automatically created if not exists.
	 * @see setCacheTableName
	 */
	public function setAutoCreateCacheTable($value)
	{
		$this->_autoCreate=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * Retrieves a value from cache with a specified key.
	 * This is the implementation of the method declared in the parent class.
	 * @param string a unique key identifying the cached value
	 * @return string the value stored in cache, false if the value is not in the cache or expired.
	 */
	protected function getValue($key)
	{
		if(!$this->_cacheInitialized) $this->initializeCache();
		try {
			$sql='SELECT value FROM '.$this->_cacheTable.' WHERE itemkey=\''.$key.'\' AND (expire=0 OR expire>'.time().') ORDER BY expire DESC';
			$command=$this->getDbConnection()->createCommand($sql);
			return $command->queryScalar();
		}
		catch(Exception $e)
		{
			$this->initializeCache(true);
			return $command->queryScalar();
		}
	}

	/**
	 * Stores a value identified by a key in cache.
	 * This is the implementation of the method declared in the parent class.
	 *
	 * @param string the key identifying the value to be cached
	 * @param string the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	protected function setValue($key,$value,$expire)
	{
		$this->deleteValue($key);
		return $this->addValue($key,$value,$expire);
	}

	/**
	 * Stores a value identified by a key into cache if the cache does not contain this key.
	 * This is the implementation of the method declared in the parent class.
	 *
	 * @param string the key identifying the value to be cached
	 * @param string the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	protected function addValue($key,$value,$expire)
	{
		if(!$this->_cacheInitialized) $this->initializeCache();
		$expire=($expire<=0)?0:time()+$expire;
		$sql="INSERT INTO {$this->_cacheTable} (itemkey,value,expire) VALUES(:key,:value,$expire)";
		try
		{
			$command=$this->getDbConnection()->createCommand($sql);
			$command->bindValue(':key',$key,PDO::PARAM_STR);
			$command->bindValue(':value',$value,PDO::PARAM_LOB);
			$command->execute();
			return true;
		}
		catch(Exception $e)
		{
			try
			{
				$this->initializeCache(true);
				$command->execute();
				return true;
			}
			catch(Exception $e)
			{
				return false;
			}
		}
	}

	/**
	 * Deletes a value with the specified key from cache
	 * This is the implementation of the method declared in the parent class.
	 * @param string the key of the value to be deleted
	 * @return boolean if no error happens during deletion
	 */
	protected function deleteValue($key)
	{
		if(!$this->_cacheInitialized) $this->initializeCache();
		try
		{
			$command=$this->getDbConnection()->createCommand("DELETE FROM {$this->_cacheTable} WHERE itemkey=:key");
			$command->bindValue(':key',$key,PDO::PARAM_STR);
			$command->execute();
			return true;
		}
		catch(Exception $e)
		{
			$this->initializeCache(true);
			$command->execute();
			return true;
		}
	}

	/**
	 * Deletes all values from cache.
	 * Be careful of performing this operation if the cache is shared by multiple applications.
	 */
	public function flush()
	{
		if(!$this->_cacheInitialized) $this->initializeCache();
		try
		{
			$command = $this->getDbConnection()->createCommand("DELETE FROM {$this->_cacheTable}");
			$command->execute();
		}
		catch(Exception $e)
		{
			try
			{
				$this->initializeCache(true);
				$command->execute();
				return true;
			}
			catch(Exception $e)
			{
				return false;
			}
		}
		return true;
	}
}
