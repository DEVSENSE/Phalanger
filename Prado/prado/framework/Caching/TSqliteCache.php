<?php
/**
 * TSqliteCache class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqliteCache.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Caching
 */

/**
 * TSqliteCache class
 *
 * TSqliteCache implements a cache application module based on SQLite database.
 *
 * To use this module, the sqlite PHP extension must be loaded. Note, Sqlite extension
 * is no longer loaded by default since PHP 5.1.
 *
 * Sine PRADO v3.1.0, a new DB-based cache module called {@link TDbCache}
 * is provided. If you have PDO extension installed, you may consider using
 * the new cache module instead as it allows you to use different database
 * to store the cached data.
 *
 * The database file is specified by the {@link setDbFile DbFile} property.
 * If not set, the database file will be created under the system state path.
 * If the specified database file does not exist, it will be created automatically.
 * Make sure the directory containing the specified DB file and the file itself is
 * writable by the Web server process.
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
 * Do not use the same database file for multiple applications using TSqliteCache.
 * Also note, cache is shared by all user sessions of an application.
 *
 * Some usage examples of TSqliteCache are as follows,
 * <code>
 * $cache=new TSqliteCache;  // TSqliteCache may also be loaded as a Prado application module
 * $cache->setDbFile($dbFilePath);
 * $cache->init(null);
 * $cache->add('object',$object);
 * $object2=$cache->get('object');
 * </code>
 *
 * If loaded, TSqliteCache will register itself with {@link TApplication} as the
 * cache module. It can be accessed via {@link TApplication::getCache()}.
 *
 * TSqliteCache may be configured in application configuration file as follows
 * <code>
 * <module id="cache" class="System.Caching.TSqliteCache" DbFile="Application.Data.site" />
 * </code>
 * where {@link getDbFile DbFile} is a property specifying the location of the
 * SQLite DB file (in the namespace format).
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TSqliteCache.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Caching
 * @since 3.0
 */
class TSqliteCache extends TCache
{
	/**
	 * name of the table storing cache data
	 */
	const CACHE_TABLE='cache';
	/**
	 * extension of the db file name
	 */
	const DB_FILE_EXT='.db';

	/**
	 * @var boolean if the module has been initialized
	 */
	private $_initialized=false;
	/**
	 * @var SQLiteDatabase the sqlite database instance
	 */
	private $_db=null;
	/**
	 * @var string the database file name
	 */
	private $_file=null;

	/**
	 * Destructor.
	 * Disconnect the db connection.
	 */
	public function __destruct()
	{
		$this->_db=null;
	}

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface. It checks if the DbFile
	 * property is set, and creates a SQLiteDatabase instance for it.
	 * The database or the cache table does not exist, they will be created.
	 * Expired values are also deleted.
	 * @param TXmlElement configuration for this module, can be null
	 * @throws TConfigurationException if sqlite extension is not installed,
	 *         DbFile is set invalid, or any error happens during creating database or cache table.
	 */
	public function init($config)
	{
		if(!function_exists('sqlite_open'))
			throw new TConfigurationException('sqlitecache_extension_required');
		if($this->_file===null)
			$this->_file=$this->getApplication()->getRuntimePath().'/sqlite.cache';
		$error='';
		if(($this->_db=new SQLiteDatabase($this->_file,0666,$error))===false)
			throw new TConfigurationException('sqlitecache_connection_failed',$error);
		if(@$this->_db->query('DELETE FROM '.self::CACHE_TABLE.' WHERE expire<>0 AND expire<'.time())===false)
		{
			if($this->_db->query('CREATE TABLE '.self::CACHE_TABLE.' (key CHAR(128) PRIMARY KEY, value BLOB, expire INT)')===false)
				throw new TConfigurationException('sqlitecache_table_creation_failed',sqlite_error_string(sqlite_last_error()));
		}
		$this->_initialized=true;
		parent::init($config);
	}

	/**
	 * @return string database file path (in namespace form)
	 */
	public function getDbFile()
	{
		return $this->_file;
	}

	/**
	 * @param string database file path (in namespace form)
	 * @throws TInvalidOperationException if the module is already initialized
	 * @throws TConfigurationException if the file is not in proper namespace format
	 */
	public function setDbFile($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('sqlitecache_dbfile_unchangeable');
		else if(($this->_file=Prado::getPathOfNamespace($value,self::DB_FILE_EXT))===null)
			throw new TConfigurationException('sqlitecache_dbfile_invalid',$value);
	}

	/**
	 * Retrieves a value from cache with a specified key.
	 * This is the implementation of the method declared in the parent class.
	 * @param string a unique key identifying the cached value
	 * @return string the value stored in cache, false if the value is not in the cache or expired.
	 */
	protected function getValue($key)
	{
		$sql='SELECT value FROM '.self::CACHE_TABLE.' WHERE key=\''.$key.'\' AND (expire=0 OR expire>'.time().') LIMIT 1';
		if(($ret=$this->_db->query($sql))!=false && ($row=$ret->fetch(SQLITE_ASSOC))!==false)
			return $row['value'];
		else
			return false;
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
		$expire=($expire<=0)?0:time()+$expire;
		$sql='REPLACE INTO '.self::CACHE_TABLE.' VALUES(\''.$key.'\',\''.sqlite_escape_string($value).'\','.$expire.')';
		return $this->_db->query($sql)!==false;
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
		$expire=($expire<=0)?0:time()+$expire;
		$sql='INSERT INTO '.self::CACHE_TABLE.' VALUES(\''.$key.'\',\''.sqlite_escape_string($value).'\','.$expire.')';
		return @$this->_db->query($sql)!==false;
	}

	/**
	 * Deletes a value with the specified key from cache
	 * This is the implementation of the method declared in the parent class.
	 * @param string the key of the value to be deleted
	 * @return boolean if no error happens during deletion
	 */
	protected function deleteValue($key)
	{
		$sql='DELETE FROM '.self::CACHE_TABLE.' WHERE key=\''.$key.'\'';
		return $this->_db->query($sql)!==false;
	}

	/**
	 * Deletes all values from cache.
	 * Be careful of performing this operation if the cache is shared by multiple applications.
	 */
	public function flush()
	{
		return $this->_db->query('DELETE FROM '.self::CACHE_TABLE)!==false;
	}
}

