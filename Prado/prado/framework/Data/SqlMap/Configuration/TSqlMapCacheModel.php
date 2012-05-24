<?php
/**
 * TSqlMapCacheModel, TSqlMapCacheTypes and TSqlMapCacheKey classes file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqlMapCacheModel.php 2757 2010-01-15 14:47:40Z Christophe.Boulain $
 * @package System.Data.SqlMap.Configuration
 */

/**
 * TSqlMapCacheModel corresponds to the <cacheModel> sql mapping configuration tag.
 *
 * The results from a query Mapped Statement can be cached simply by specifying
 * the {@link CacheModel TSqlMapStatement::setCacheModel()} property in <statement> tag.
 * A cache model is a configured cache that is defined within the sql map
 * configuration file. Cache models are configured using the <cacheModel> element.
 *
 * The cache model uses a pluggable framework for supporting different types of
 * caches. The choice of cache is specified by the {@link Implementation setImplementation()}
 * property. The class name specified must be one of {@link TSqlMapCacheTypes}.
 *
 * The cache implementations, LRU and FIFO cache below do not persist across
 * requests. That is, once the request is complete, all cache data is lost.
 * These caches are useful queries that results in the same repeated data during
 * the current request.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapCacheModel.php 2757 2010-01-15 14:47:40Z Christophe.Boulain $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapCacheModel extends TComponent
{
	private $_cache;
	private $_hits = 0;
	private $_requests = 0;
	private $_id;
	private $_implementation=TSqlMapCacheTypes::Basic;
	private $_properties = array();
	private $_flushInterval = 0;

	/**
	 * @return string unique cache model identifier.
	 */
	public function getID()
	{
		return $this->_id;
	}

	/**
	 * @param string unique cache model identifier.
	 */
	public function setID($value)
	{
		$this->_id = $value;
	}

	/**
	 * @return string cache implements of TSqlMapCacheTypes, either 'Basic', 'LRU' or 'FIFO'.
	 */
	public function getImplementation()
	{
		return $this->_implementation;
	}

	/**
	 * @param string cache implements of TSqlMapCacheTypes, either 'Basic', 'LRU' or 'FIFO'.
	 */
	public function setImplementation($value)
	{
		$this->_implementation = TPropertyValue::ensureEnum($value,'TSqlMapCacheTypes');
	}

	/**
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 */
	public function setFlushInterval($value)
	{
		$this->_flushInterval=TPropertyValue::ensureInteger($value);
	}

	/**
	 * @return integer cache duration.
	 */
	public function getFlushInterval()
	{
		return $this->_flushInterval;
	}

	/**
	 * Initialize the cache implementation, sets the actual cache contain if supplied.
	 * @param ISqLMapCache cache implementation instance.
	 */
	public function initialize($cache=null)
	{
		if($cache===null)
			$this->_cache= Prado::createComponent($this->getImplementationClass(), $this);
		else
			$this->_cache=$cache;
	}

	/**
	 * @return string cache implementation class name.
	 */
	public function getImplementationClass()
	{
		switch(TPropertyValue::ensureEnum($this->_implementation,'TSqlMapCacheTypes'))
		{
			case TSqlMapCacheTypes::FIFO: return 'TSqlMapFifoCache';
			case TSqlMapCacheTypes::LRU : return 'TSqlMapLruCache';
			case TSqlMapCacheTypes::Basic : return 'TSqlMapApplicationCache';
		}
	}

	/**
	 * Register a mapped statement that will trigger a cache flush.
	 * @param TMappedStatement mapped statement that may flush the cache.
	 */
	public function registerTriggerStatement($mappedStatement)
	{
		$mappedStatement->attachEventHandler('OnExecuteQuery',array($this, 'flush'));
	}

	/**
	 * Clears the cache.
	 */
	public function flush()
	{
		$this->_cache->flush();
	}

	/**
	 * @param TSqlMapCacheKey|string cache key
	 * @return mixed cached value.
	 */
	public function get($key)
	{
		if($key instanceof TSqlMapCacheKey)
			$key = $key->getHash();

		//if flush ?
		$value = $this->_cache->get($key);
		$this->_requests++;
		if($value!==null)
			$this->_hits++;
		return $value;
	}

	/**
	 * @param TSqlMapCacheKey|string cache key
	 * @param mixed value to be cached.
	 */
	public function set($key, $value)
	{
		if($key instanceof TSqlMapCacheKey)
			$key = $key->getHash();

		if($value!==null)
			$this->_cache->set($key, $value, $this->_flushInterval);
	}

	/**
	 * @return float cache hit ratio.
	 */
	public function getHitRatio()
	{
		if($this->_requests != 0)
			return $this->_hits / $this->_requests;
		else
			return 0;
	}
}

/**
 * TSqlMapCacheTypes enumerable class.
 *
 * Implemented cache are 'Basic', 'FIFO' and 'LRU'.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapCacheModel.php 2757 2010-01-15 14:47:40Z Christophe.Boulain $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapCacheTypes extends TEnumerable
{
	const Basic='Basic';
	const FIFO='FIFO';
	const LRU='LRU';
}

/**
 * TSqlMapCacheKey class.
 *
 * Provides a hash of the object to be cached.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapCacheModel.php 2757 2010-01-15 14:47:40Z Christophe.Boulain $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapCacheKey
{
	private $_key;

	/**
	 * @param mixed object to be cached.
	 */
	public function __construct($object)
	{
		$this->_key = $this->generateKey(serialize($object));
	}

	/**
	 * @param string serialized object
	 * @return string crc32 hash of the serialized object.
	 */
	protected function generateKey($string)
	{
		return sprintf('%x',crc32($string));
	}

	/**
	 * @return string object hash.
	 */
	public function getHash()
	{
		return $this->_key;
	}
}

