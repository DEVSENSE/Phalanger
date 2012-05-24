<?php
/**
 * TCache and cache dependency classes.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 */

Prado::using('System.Collections.TList');

/**
 * TCache class
 *
 * TCache is the base class for cache classes with different cache storage implementation.
 *
 * TCache implements the interface {@link ICache} with the following methods,
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
 * Child classes must implement the following methods:
 * - {@link getValue}
 * - {@link setValue}
 * - {@link addValue}
 * - {@link deleteValue}
 * and optionally {@link flush}
 *
 * Since version 3.1.2, TCache implements the ArrayAccess interface such that
 * the cache acts as an array.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.0
 */
abstract class TCache extends TModule implements ICache, ArrayAccess
{
	private $_prefix=null;
	private $_primary=true;

	/**
	 * Initializes the cache module.
	 * This method initializes the cache key prefix and registers the cache module
	 * with the application if the cache is primary.
	 * @param TXmlElement the module configuration
	 */
	public function init($config)
	{
		if($this->_prefix===null)
			$this->_prefix=$this->getApplication()->getUniqueID();
		if($this->_primary)
		{
			if($this->getApplication()->getCache()===null)
				$this->getApplication()->setCache($this);
			else
				throw new TConfigurationException('cache_primary_duplicated',get_class($this));
		}
	}

	/**
	 * @return boolean whether this cache module is used as primary/system cache.
	 * A primary cache is used by PRADO core framework to cache data such as
	 * parsed templates, themes, etc.
	 */
	public function getPrimaryCache()
	{
		return $this->_primary;
	}

	/**
	 * @param boolean whether this cache module is used as primary/system cache. Defaults to false.
	 * @see getPrimaryCache
	 */
	public function setPrimaryCache($value)
	{
		$this->_primary=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return string a unique prefix for the keys of cached values.
	 * If it is not explicitly set, it will take the value of {@link TApplication::getUniqueID}.
	 */
	public function getKeyPrefix()
	{
		return $this->_prefix;
	}

	/**
	 * @param string a unique prefix for the keys of cached values
	 */
	public function setKeyPrefix($value)
	{
		$this->_prefix=$value;
	}

	/**
	 * @param string a key identifying a value to be cached
	 * @return sring a key generated from the provided key which ensures the uniqueness across applications
	 */
	protected function generateUniqueKey($key)
	{
		return md5($this->_prefix.$key);
	}

	/**
	 * Retrieves a value from cache with a specified key.
	 * @param string a key identifying the cached value
	 * @return mixed the value stored in cache, false if the value is not in the cache or expired.
	 */
	public function get($id)
	{
		if(($value=$this->getValue($this->generateUniqueKey($id)))!==false)
		{
			$data=unserialize($value);
			if(!is_array($data))
				return false;
			if(!($data[1] instanceof ICacheDependency) || !$data[1]->getHasChanged())
				return $data[0];
		}
		return false;
	}

	/**
	 * Stores a value identified by a key into cache.
	 * If the cache already contains such a key, the existing value and
	 * expiration time will be replaced with the new ones. If the value is
	 * empty, the cache key will be deleted.
	 *
	 * @param string the key identifying the value to be cached
	 * @param mixed the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @param ICacheDependency dependency of the cached item. If the dependency changes, the item is labeled invalid.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	public function set($id,$value,$expire=0,$dependency=null)
	{
		if(empty($value) && $expire === 0)
			$this->delete($id);
		else
		{
			$data=array($value,$dependency);
			return $this->setValue($this->generateUniqueKey($id),serialize($data),$expire);
		}
	}

	/**
	 * Stores a value identified by a key into cache if the cache does not contain this key.
	 * Nothing will be done if the cache already contains the key or if value is empty.
	 * @param string the key identifying the value to be cached
	 * @param mixed the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @param ICacheDependency dependency of the cached item. If the dependency changes, the item is labeled invalid.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	public function add($id,$value,$expire=0,$dependency=null)
	{
		if(empty($value) && $expire === 0)
			return false;
		$data=array($value,$dependency);
		return $this->addValue($this->generateUniqueKey($id),serialize($data),$expire);
	}

	/**
	 * Deletes a value with the specified key from cache
	 * @param string the key of the value to be deleted
	 * @return boolean if no error happens during deletion
	 */
	public function delete($id)
	{
		return $this->deleteValue($this->generateUniqueKey($id));
	}

	/**
	 * Deletes all values from cache.
	 * Be careful of performing this operation if the cache is shared by multiple applications.
	 * Child classes may implement this method to realize the flush operation.
	 * @throws TNotSupportedException if this method is not overridden by child classes
	 */
	public function flush()
	{
		throw new TNotSupportedException('cache_flush_unsupported');
	}

	/**
	 * Retrieves a value from cache with a specified key.
	 * This method should be implemented by child classes to store the data
	 * in specific cache storage. The uniqueness and dependency are handled
	 * in {@link get()} already. So only the implementation of data retrieval
	 * is needed.
	 * @param string a unique key identifying the cached value
	 * @return string the value stored in cache, false if the value is not in the cache or expired.
	 */
	abstract protected function getValue($key);

	/**
	 * Stores a value identified by a key in cache.
	 * This method should be implemented by child classes to store the data
	 * in specific cache storage. The uniqueness and dependency are handled
	 * in {@link set()} already. So only the implementation of data storage
	 * is needed.
	 *
	 * @param string the key identifying the value to be cached
	 * @param string the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	abstract protected function setValue($key,$value,$expire);

	/**
	 * Stores a value identified by a key into cache if the cache does not contain this key.
	 * This method should be implemented by child classes to store the data
	 * in specific cache storage. The uniqueness and dependency are handled
	 * in {@link add()} already. So only the implementation of data storage
	 * is needed.
	 *
	 * @param string the key identifying the value to be cached
	 * @param string the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	abstract protected function addValue($key,$value,$expire);

	/**
	 * Deletes a value with the specified key from cache
	 * This method should be implemented by child classes to delete the data from actual cache storage.
	 * @param string the key of the value to be deleted
	 * @return boolean if no error happens during deletion
	 */
	abstract protected function deleteValue($key);

	/**
	 * Returns whether there is a cache entry with a specified key.
	 * This method is required by the interface ArrayAccess.
	 * @param string a key identifying the cached value
	 * @return boolean
	 */
	public function offsetExists($id)
	{
		return $this->get($id) !== false;
	}

	/*
	 * Retrieves the value from cache with a specified key.
	 * This method is required by the interface ArrayAccess.
	 * @param string a key identifying the cached value
	 * @return mixed the value stored in cache, false if the value is not in the cache or expired.
	 */
	public function offsetGet($id)
	{
		return $this->get($id);
	}

	/*
	 * Stores the value identified by a key into cache.
	 * If the cache already contains such a key, the existing value will be
	 * replaced with the new ones. To add expiration and dependencies, use the set() method.
	 * This method is required by the interface ArrayAccess.
	 * @param string the key identifying the value to be cached
	 * @param mixed the value to be cached
	 */
	public function offsetSet($id, $value)
	{
		$this->set($id, $value);
	}

	/*
	 * Deletes the value with the specified key from cache
	 * This method is required by the interface ArrayAccess.
	 * @param string the key of the value to be deleted
	 * @return boolean if no error happens during deletion
	 */
	public function offsetUnset($id)
	{
		$this->delete($id);
	}
}


/**
 * TCacheDependency class.
 *
 * TCacheDependency is the base class implementing {@link ICacheDependency} interface.
 * Descendant classes must implement {@link getHasChanged()} to provide
 * actual dependency checking logic.
 *
 * The property value of {@link getHasChanged HasChanged} tells whether
 * the dependency is changed or not.
 *
 * You may disable the dependency checking by setting {@link setEnabled Enabled}
 * to false.
 *
 * Note, since the dependency objects often need to be serialized so that
 * they can persist across requests, you may need to implement __sleep() and
 * __wakeup() if the dependency objects contain resource handles which are
 * not serializable.
 *
 * Currently, the following dependency classes are provided in the PRADO release:
 * - {@link TFileCacheDependency}: checks whether a file is changed or not
 * - {@link TDirectoryCacheDependency}: checks whether a directory is changed or not
 * - {@link TGlobalStateCacheDependency}: checks whether a global state is changed or not
 * - {@link TChainedCacheDependency}: checks whether any of a list of dependencies is changed or not
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.1.0
 */
abstract class TCacheDependency extends TComponent implements ICacheDependency
{
}


/**
 * TFileCacheDependency class.
 *
 * TFileCacheDependency performs dependency checking based on the
 * last modification time of the file specified via {@link setFileName FileName}.
 * The dependency is reported as unchanged if and only if the file's
 * last modification time remains unchanged.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.1.0
 */
class TFileCacheDependency extends TCacheDependency
{
	private $_fileName;
	private $_timestamp;

	/**
	 * Constructor.
	 * @param string name of the file whose change is to be checked.
	 */
	public function __construct($fileName)
	{
		$this->setFileName($fileName);
	}

	/**
	 * @return string the name of the file whose change is to be checked
	 */
	public function getFileName()
	{
		return $this->_fileName;
	}

	/**
	 * @param string the name of the file whose change is to be checked
	 */
	public function setFileName($value)
	{
		$this->_fileName=$value;
		$this->_timestamp=@filemtime($value);
	}

	/**
	 * @return int the last modification time of the file
	 */
	public function getTimestamp()
	{
		return $this->_timestamp;
	}

	/**
	 * Performs the actual dependency checking.
	 * This method returns true if the last modification time of the file is changed.
	 * @return boolean whether the dependency is changed or not.
	 */
	public function getHasChanged()
	{
		return @filemtime($this->_fileName)!==$this->_timestamp;
	}
}

/**
 * TDirectoryCacheDependency class.
 *
 * TDirectoryCacheDependency performs dependency checking based on the
 * modification time of the files contained in the specified directory.
 * The directory being checked is specified via {@link setDirectory Directory}.
 *
 * By default, all files under the specified directory and subdirectories
 * will be checked. If the last modification time of any of them is changed
 * or if different number of files are contained in a directory, the dependency
 * is reported as changed. By specifying {@link setRecursiveCheck RecursiveCheck}
 * and {@link setRecursiveLevel RecursiveLevel}, one can limit the checking
 * to a certain depth of the subdirectories.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.1.0
 */
class TDirectoryCacheDependency extends TCacheDependency
{
	private $_recursiveCheck=true;
	private $_recursiveLevel=-1;
	private $_timestamps;
	private $_directory;

	/**
	 * Constructor.
	 * @param string the directory to be checked
	 */
	public function __construct($directory)
	{
		$this->setDirectory($directory);
	}

	/**
	 * @return string the directory to be checked
	 */
	public function getDirectory()
	{
		return $this->_directory;
	}

	/**
	 * @param string the directory to be checked
	 * @throws TInvalidDataValueException if the directory does not exist
	 */
	public function setDirectory($directory)
	{
		if(($path=realpath($directory))===false || !is_dir($path))
			throw new TInvalidDataValueException('directorycachedependency_directory_invalid',$directory);
		$this->_directory=$path;
		$this->_timestamps=$this->generateTimestamps($path);
	}

	/**
	 * @return boolean whether the subdirectories of the directory will also be checked.
	 * It defaults to true.
	 */
	public function getRecursiveCheck()
	{
		return $this->_recursiveCheck;
	}

	/**
	 * @param boolean whether the subdirectories of the directory will also be checked.
	 */
	public function setRecursiveCheck($value)
	{
		$this->_recursiveCheck=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return int the depth of the subdirectories to be checked.
	 * It defaults to -1, meaning unlimited depth.
	 */
	public function getRecursiveLevel()
	{
		return $this->_recursiveLevel;
	}

	/**
	 * Sets a value indicating the depth of the subdirectories to be checked.
	 * This is meaningful only when {@link getRecursiveCheck RecursiveCheck}
	 * is true.
	 * @param int the depth of the subdirectories to be checked.
	 * If the value is less than 0, it means unlimited depth.
	 * If the value is 0, it means checking the files directly under the specified directory.
	 */
	public function setRecursiveLevel($value)
	{
		$this->_recursiveLevel=TPropertyValue::ensureInteger($value);
	}

	/**
	 * Performs the actual dependency checking.
	 * This method returns true if the directory is changed.
	 * @return boolean whether the dependency is changed or not.
	 */
	public function getHasChanged()
	{
		return $this->generateTimestamps($this->_directory)!=$this->_timestamps;
	}

	/**
	 * Checks to see if the file should be checked for dependency.
	 * This method is invoked when dependency of the whole directory is being checked.
	 * By default, it always returns true, meaning the file should be checked.
	 * You may override this method to check only certain files.
	 * @param string the name of the file that may be checked for dependency.
	 * @return boolean whether this file should be checked.
	 */
	protected function validateFile($fileName)
	{
		return true;
	}

	/**
	 * Checks to see if the specified subdirectory should be checked for dependency.
	 * This method is invoked when dependency of the whole directory is being checked.
	 * By default, it always returns true, meaning the subdirectory should be checked.
	 * You may override this method to check only certain subdirectories.
	 * @param string the name of the subdirectory that may be checked for dependency.
	 * @return boolean whether this subdirectory should be checked.
	 */
	protected function validateDirectory($directory)
	{
		return true;
	}

	/**
	 * Determines the last modification time for files under the directory.
	 * This method may go recursively into subdirectories if
	 * {@link setRecursiveCheck RecursiveCheck} is set true.
	 * @param string the directory name
	 * @param int level of the recursion
	 * @return array list of file modification time indexed by the file path
	 */
	protected function generateTimestamps($directory,$level=0)
	{
		if(($dir=opendir($directory))===false)
			throw new TIOException('directorycachedependency_directory_invalid',$directory);
		$timestamps=array();
		while(($file=readdir($dir))!==false)
		{
			$path=$directory.DIRECTORY_SEPARATOR.$file;
			if($file==='.' || $file==='..')
				continue;
			else if(is_dir($path))
			{
				if(($this->_recursiveLevel<0 || $level<$this->_recursiveLevel) && $this->validateDirectory($path))
					$timestamps=array_merge($this->generateTimestamps($path,$level+1));
			}
			else if($this->validateFile($path))
				$timestamps[$path]=filemtime($path);
		}
		closedir($dir);
		return $timestamps;
	}
}


/**
 * TGlobalStateCacheDependency class.
 *
 * TGlobalStateCacheDependency checks if a global state is changed or not.
 * If the global state is changed, the dependency is reported as changed.
 * To specify which global state this dependency should check with,
 * set {@link setStateName StateName} to the name of the global state.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.1.0
 */
class TGlobalStateCacheDependency extends TCacheDependency
{
	private $_stateName;
	private $_stateValue;

	/**
	 * Constructor.
	 * @param string the name of the global state
	 */
	public function __construct($name)
	{
		$this->setStateName($name);
	}

	/**
	 * @return string the name of the global state
	 */
	public function getStateName()
	{
		return $this->_stateName;
	}

	/**
	 * @param string the name of the global state
	 * @see TApplication::setGlobalState
	 */
	public function setStateName($value)
	{
		$this->_stateName=$value;
		$this->_stateValue=Prado::getApplication()->getGlobalState($value);
	}

	/**
	 * Performs the actual dependency checking.
	 * This method returns true if the specified global state is changed.
	 * @return boolean whether the dependency is changed or not.
	 */
	public function getHasChanged()
	{
		return $this->_stateValue!==Prado::getApplication()->getGlobalState($this->_stateName);
	}
}


/**
 * TChainedCacheDependency class.
 *
 * TChainedCacheDependency represents a list of cache dependency objects
 * and performs the dependency checking based on the checking results of
 * these objects. If any of them reports a dependency change, TChainedCacheDependency
 * will return true for the checking.
 *
 * To add dependencies to TChainedCacheDependency, use {@link getDependencies Dependencies}
 * which gives a {@link TCacheDependencyList} instance and can be used like an array
 * (see {@link TList} for more details}).
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.1.0
 */
class TChainedCacheDependency extends TCacheDependency
{
	private $_dependencies=null;

	/**
	 * @return TCacheDependencyList list of dependency objects
	 */
	public function getDependencies()
	{
		if($this->_dependencies===null)
			$this->_dependencies=new TCacheDependencyList;
		return $this->_dependencies;
	}

	/**
	 * Performs the actual dependency checking.
	 * This method returns true if any of the dependency objects
	 * reports a dependency change.
	 * @return boolean whether the dependency is changed or not.
	 */
	public function getHasChanged()
	{
		if($this->_dependencies!==null)
		{
			foreach($this->_dependencies as $dependency)
				if($dependency->getHasChanged())
					return true;
		}
		return false;
	}
}


/**
 * TApplicationStateCacheDependency class.
 *
 * TApplicationStateCacheDependency performs dependency checking based on
 * the mode of the currently running PRADO application.
 * The dependency is reportedly as unchanged if and only if the application
 * is running in performance mode.
 *
 * You may chain this dependency together with other dependencies
 * so that only when the application is not in performance mode the other dependencies
 * will be checked.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.1.0
 */
class TApplicationStateCacheDependency extends TCacheDependency
{
	/**
	 * Performs the actual dependency checking.
	 * This method returns true if the currently running application is not in performance mode.
	 * @return boolean whether the dependency is changed or not.
	 */
	public function getHasChanged()
	{
		return Prado::getApplication()->getMode()!==TApplicationMode::Performance;
	}
}

/**
 * TCacheDependencyList class.
 *
 * TCacheDependencyList represents a list of cache dependency objects.
 * Only objects implementing {@link ICacheDependency} can be added into this list.
 *
 * TCacheDependencyList can be used like an array. See {@link TList}
 * for more details.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCache.php 2513 2008-10-13 10:46:23Z carl $
 * @package System.Caching
 * @since 3.1.0
 */
class TCacheDependencyList extends TList
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by performing additional type checking
	 * for each newly added item.
	 * @param integer the specified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a dependency instance
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof ICacheDependency)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('cachedependencylist_cachedependency_required');
	}
}

?>
