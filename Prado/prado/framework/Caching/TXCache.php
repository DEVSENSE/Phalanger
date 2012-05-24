<?php
/**
 * TXCache class file
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TXCache.php 1994 2007-06-11 16:02:28Z knut $
 * @package System.Caching
 */

/**
 * TXCache class
 *
 * TXCache implements a cache application module based on {@link http://xcache.lighttpd.net/ xcache}.
 *
 * By definition, cache does not ensure the existence of a value
 * even if it never expires. Cache is not meant to be an persistent storage.
 *
 * To use this module, the xcache PHP extension must be loaded and configured in the php.ini.
 *
 * Some usage examples of TXCache are as follows,
 * <code>
 * $cache=new TXCache;  // TXCache may also be loaded as a Prado application module
 * $cache->init(null);
 * $cache->add('object',$object);
 * $object2=$cache->get('object');
 * </code>
 *
 * If loaded, TXCache will register itself with {@link TApplication} as the
 * cache module. It can be accessed via {@link TApplication::getCache()}.
 *
 * TXCache may be configured in application configuration file as follows
 * <code>
 * <module id="cache" class="System.Caching.TXCache" />
 * </code>
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TXCache.php 1994 2007-06-11 16:02:28Z knut $
 * @package System.Caching
 * @since 3.1.1
 */
class TXCache extends TCache
{
   /**
    * Initializes this module.
    * This method is required by the IModule interface.
    * @param TXmlElement configuration for this module, can be null
    * @throws TConfigurationException if xcache extension is not installed or not started, check your php.ini
    */
	public function init($config)
	{
		if(!function_exists('xcache_isset'))
			throw new TConfigurationException('xcache_extension_required');

		$enabled = (int)ini_get('xcache.cacher') !== 0;
		$var_size = (int)ini_get('xcache.var_size');

		if(!($enabled && $var_size > 0))
			throw new TConfigurationException('xcache_extension_not_enabled');

		parent::init($config);
	}

	/**
	 * Retrieves a value from cache with a specified key.
	 * This is the implementation of the method declared in the parent class.
	 * @param string a unique key identifying the cached value
	 * @return string the value stored in cache, false if the value is not in the cache or expired.
	 */
	protected function getValue($key)
	{
		return xcache_isset($key) ? xcache_get($key) : false;
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
		return xcache_set($key,$value,$expire);
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
		return !xcache_isset($key) ? $this->setValue($key,$value,$expire) : false;
	}

	/**
	 * Deletes a value with the specified key from cache
	 * This is the implementation of the method declared in the parent class.
	 * @param string the key of the value to be deleted
	 * @return boolean if no error happens during deletion
	 */
	protected function deleteValue($key)
	{
		return xcache_unset($key);
	}

	/**
	 * Deletes all values from cache.
	 * Be careful of performing this operation if the cache is shared by multiple applications.
	 */
	public function flush()
	{
		return xcache_clear_cache();
	}
}

?>
