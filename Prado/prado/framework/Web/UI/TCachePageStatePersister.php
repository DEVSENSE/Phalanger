<?php
/**
 * TCachePageStatePersister class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TCachePageStatePersister.php 3008 2011-07-06 12:50:13Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 */

/**
 * TCachePageStatePersister class
 *
 * TCachePageStatePersister implements a page state persistent method based on cache.
 * Page state are stored in cache (e.g. memcache, DB cache, etc.), and only a small token
 * is passed to the client side to identify the state. This greatly reduces the size of
 * the page state that needs to be transmitted between the server and the client. Of course,
 * this is at the cost of using server side resource.
 *
 * A cache module has to be loaded in order to use TCachePageStatePersister.
 * By default, TCachePageStatePersister will use the primary cache module.
 * A non-primary cache module can be used by setting {@link setCacheModuleID CacheModuleID}.
 * Any cache module, as long as it implements the interface {@link ICache}, may be used.
 * For example, one can use {@link TDbCache}, {@link TMemCache}, {@link TAPCCache}, etc.
 *
 * TCachePageStatePersister uses {@link setCacheTimeout CacheTimeout} to limit the data
 * that stores in cache.
 *
 * Since server resource is often limited, be cautious if you plan to use TCachePageStatePersister
 * for high-traffic Web pages. You may consider using a small {@link setCacheTimeout CacheTimeout}.
 *
 * There are a couple of ways to use TCachePageStatePersister.
 * One can override the page's {@link TPage::getStatePersister()} method and
 * create a TCachePageStatePersister instance there.
 * Or one can configure the pages to use TCachePageStatePersister in page configurations
 * as follows,
 * <code>
 *   <pages StatePersisterClass="System.Web.UI.TCachePageStatePersister"
 *          StatePersister.CacheModuleID="mycache"
 *          StatePersister.CacheTimeout="3600" />
 * </code>
 * Note in the above, we use StatePersister.CacheModuleID to configure the cache module ID
 * for the TCachePageStatePersister instance.
 *
 * The above configuration will affect the pages under the directory containing
 * this configuration and all its subdirectories.
 * To configure individual pages to use TCachePageStatePersister, use
 * <code>
 *   <pages>
 *     <page id="PageID" StatePersisterClass="System.Web.UI.TCachePageStatePersister" />
 *   </pages>
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCachePageStatePersister.php 3008 2011-07-06 12:50:13Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 * @since 3.1.1
 */
class TCachePageStatePersister extends TComponent implements IPageStatePersister
{
	private $_prefix='statepersister';
	private $_page;
	private $_cache=null;
	private $_cacheModuleID='';
	private $_timeout=1800;

	/**
	 * @param TPage the page that this persister works for
	 */
	public function getPage()
	{
		return $this->_page;
	}

	/**
	 * @param TPage the page that this persister works for.
	 */
	public function setPage(TPage $page)
	{
		$this->_page=$page;
	}

	/**
	 * @return string the ID of the cache module.
	 */
	public function getCacheModuleID()
	{
		return $this->_cacheModuleID;
	}

	/**
	 * @param string the ID of the cache module. If not set, the primary cache module will be used.
	 */
	public function setCacheModuleID($value)
	{
		$this->_cacheModuleID=$value;
	}

	/**
	 * @return ICache the cache module being used for data storage
	 */
	public function getCache()
	{
		if($this->_cache===null)
		{
			if($this->_cacheModuleID!=='')
				$cache=Prado::getApplication()->getModule($this->_cacheModuleID);
			else
				$cache=Prado::getApplication()->getCache();
			if($cache===null || !($cache instanceof ICache))
			{
				if($this->_cacheModuleID!=='')
					throw new TConfigurationException('cachepagestatepersister_cachemoduleid_invalid',$this->_cacheModuleID);
				else
					throw new TConfigurationException('cachepagestatepersister_cache_required');
			}
			$this->_cache=$cache;
		}
		return $this->_cache;
	}

	/**
	 * @return integer the number of seconds in which the cached state will expire. Defaults to 1800.
	 */
	public function getCacheTimeout()
	{
		return $this->_timeout;
	}

	/**
	 * @param integer the number of seconds in which the cached state will expire. 0 means never expire.
	 * @throws TInvalidDataValueException if the number is smaller than 0.
	 */
	public function setCacheTimeout($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))>=0)
			$this->_timeout=$value;
		else
			throw new TInvalidDataValueException('cachepagestatepersister_timeout_invalid');
	}

	/**
	 * @return string prefix of cache variable name to avoid conflict with other cache data. Defaults to 'statepersister'.
	 */
	public function getKeyPrefix()
	{
	    return $this->_prefix;
	}

	/**
     * @param string prefix of cache variable name to avoid conflict with other cache data
     */
	public function setKeyPrefix($value)
	{
	    $this->_prefix=$value;
	}

	/**
	 * @param string micro timestamp when saving state occurs
	 * @return string a key that is unique per user request
	 */
	protected function calculateKey($timestamp)
	{
		return $this->getKeyPrefix().':'
			. $this->_page->getRequest()->getUserHostAddress()
			. $this->_page->getPagePath()
			. $timestamp;
	}

	/**
	 * Saves state in cache.
	 * @param mixed state to be stored
	 */
	public function save($state)
	{
		$data=serialize($state);
		$timestamp=(string)microtime(true);
		$key=$this->calculateKey($timestamp);
		$this->getCache()->add($key,$data,$this->_timeout);
		$this->_page->setClientState(TPageStateFormatter::serialize($this->_page,$timestamp));
	}

	/**
	 * Loads page state from cache.
	 * @return mixed the restored state
	 * @throws THttpException if page state is corrupted
	 */
	public function load()
	{
		if(($timestamp=TPageStateFormatter::unserialize($this->_page,$this->_page->getRequestClientState()))!==null)
		{
			$key=$this->calculateKey($timestamp);
			if(($data=$this->getCache()->get($key))!==false)
				return unserialize($data);
		}
		throw new THttpException(400,'cachepagestatepersister_pagestate_corrupted');
	}
}

