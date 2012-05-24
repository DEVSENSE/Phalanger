<?php
/**
 * TOutputCache class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TOutputCache.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 */

/**
 * TOutputCache class.
 *
 * TOutputCache enables caching a portion of a Web page, also known as
 * partial caching. The content being cached can be either static or
 * dynamic.
 *
 * To use TOutputCache, simply enclose the content to be cached
 * within the TOutputCache component tag on a template, e.g.,
 * <code>
 * <com:TOutputCache>
 *   content to be cached
 * </com:TOutputCache>
 * </code>
 * where content to be cached can be static text and/or component tags.
 *
 * The validity of the cached content is determined based on two factors:
 * the {@link setDuration Duration} and the cache dependency.
 * The former specifies the number of seconds that the data can remain
 * valid in cache (defaults to 60s), while the latter specifies conditions
 * that the cached data depends on. If a dependency changes,
 * (e.g. relevant data in DB are updated), the cached data will be invalidated.
 *
 * There are two ways to specify cache dependency. One may write event handlers
 * to respond to the {@link onCheckDependency OnCheckDependency} event and set
 * the event parameter's {@link TOutputCacheCheckDependencyEventParameter::getIsValid IsValid}
 * property to indicate whether the cached data remains valid or not.
 * One can also extend TOutputCache and override its {@link getCacheDependency}
 * function. While the former is easier to use, the latter offers more extensibility.
 *
 * The content fetched from cache may be variated with respect to
 * some parameters. It supports variation with respect to request parameters,
 * which is specified by {@link setVaryByParam VaryByParam} property.
 * If a specified request parameter is different, a different version of
 * cached content is used. This is extremely useful if a page's content
 * may be variated according to some GET parameters.
 * The content being cached may also be variated with user sessions if
 * {@link setVaryBySession VaryBySession} is set true.
 * To variate the cached content by other factors, override {@link calculateCacheKey()} method.
 *
 * Output caches can be nested. An outer cache takes precedence over an
 * inner cache. This means, if the content cached by the inner cache expires
 * or is invalidated, while that by the outer cache not, the outer cached
 * content will be used.
 *
 * Note, TOutputCache is effective only for non-postback page requests
 * and when cache module is enabled.
 *
 * Do not attempt to address child controls of TOutputCache when the cached
 * content is to be used. Use {@link getContentCached ContentCached} property
 * to determine whether the content is cached or not.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TOutputCache.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.1
 */
class TOutputCache extends TControl implements INamingContainer
{
	const CACHE_ID_PREFIX='prado:outputcache';
	private $_cacheModuleID='';
	private $_dataCached=false;
	private $_cacheAvailable=false;
	private $_cacheChecked=false;
	private $_cacheKey=null;
	private $_duration=60;
	private $_cache=null;
	private $_contents;
	private $_state;
	private $_actions=array();
	private $_varyByParam='';
	private $_keyPrefix='';
	private $_varyBySession=false;
	private $_cachePostBack=false;
	private $_cacheTime=0;

	/**
	 * Returns a value indicating whether body contents are allowed for this control.
	 * This method overrides the parent implementation by checking if cached
	 * content is available or not. If yes, it returns false, otherwise true.
	 * @param boolean whether body contents are allowed for this control.
	 */
	public function getAllowChildControls()
	{
		$this->determineCacheability();
		return !$this->_dataCached;
	}

	private function determineCacheability()
	{
		if(!$this->_cacheChecked)
		{
			$this->_cacheChecked=true;
			if($this->_duration>0 && ($this->_cachePostBack || !$this->getPage()->getIsPostBack()))
			{
				if($this->_cacheModuleID!=='')
				{
					$this->_cache=$this->getApplication()->getModule($this->_cacheModuleID);
					if(!($this->_cache instanceof ICache))
						throw new TConfigurationException('outputcache_cachemoduleid_invalid',$this->_cacheModuleID);
				}
				else
					$this->_cache=$this->getApplication()->getCache();
				if($this->_cache!==null)
				{
					$this->_cacheAvailable=true;
					$data=$this->_cache->get($this->getCacheKey());
					if(is_array($data))
					{
						$param=new TOutputCacheCheckDependencyEventParameter;
						$param->setCacheTime(isset($data[3])?$data[3]:0);
						$this->onCheckDependency($param);
						$this->_dataCached=$param->getIsValid();
					}
					else
						$this->_dataCached=false;
					if($this->_dataCached)
						list($this->_contents,$this->_state,$this->_actions,$this->_cacheTime)=$data;
				}
			}
		}
	}

	/**
	 * Performs the Init step for the control and all its child controls.
	 * This method overrides the parent implementation by setting up
	 * the stack of the output cache in the page.
	 * Only framework developers should use this method.
	 * @param TControl the naming container control
	 */
	protected function initRecursive($namingContainer=null)
	{
		if($this->_cacheAvailable && !$this->_dataCached)
		{
			$stack=$this->getPage()->getCachingStack();
			$stack->push($this);
			parent::initRecursive($namingContainer);
			$stack->pop();
		}
		else
			parent::initRecursive($namingContainer);
	}

	/**
	 * Performs the Load step for the control and all its child controls.
	 * This method overrides the parent implementation by setting up
	 * the stack of the output cache in the page. If the data is restored
	 * from cache, it also recovers the actions associated with the cached data.
	 * Only framework developers should use this method.
	 * @param TControl the naming container control
	 */
	protected function loadRecursive()
	{
		if($this->_cacheAvailable && !$this->_dataCached)
		{
			$stack=$this->getPage()->getCachingStack();
			$stack->push($this);
			parent::loadRecursive();
			$stack->pop();
		}
		else
		{
			if($this->_dataCached)
				$this->performActions();
			parent::loadRecursive();
		}
	}

	private function performActions()
	{
		$page=$this->getPage();
		$cs=$page->getClientScript();
		foreach($this->_actions as $action)
		{
			if($action[0]==='Page.ClientScript')
				call_user_func_array(array($cs,$action[1]),$action[2]);
			else if($action[0]==='Page')
				call_user_func_array(array($page,$action[1]),$action[2]);
			else
				call_user_func_array(array($this->getSubProperty($action[0]),$action[1]),$action[2]);
		}
	}

	/**
	 * Performs the PreRender step for the control and all its child controls.
	 * This method overrides the parent implementation by setting up
	 * the stack of the output cache in the page.
	 * Only framework developers should use this method.
	 * @param TControl the naming container control
	 */
	protected function preRenderRecursive()
	{
		if($this->_cacheAvailable && !$this->_dataCached)
		{
			$stack=$this->getPage()->getCachingStack();
			$stack->push($this);
			parent::preRenderRecursive();
			$stack->pop();
		}
		else
			parent::preRenderRecursive();
	}

	/**
	 * Loads state (viewstate and controlstate) into a control and its children.
	 * This method overrides the parent implementation by loading
	 * cached state if available.
	 * This method should only be used by framework developers.
	 * @param array the collection of the state
	 * @param boolean whether the viewstate should be loaded
	 */
	protected function loadStateRecursive(&$state,$needViewState=true)
	{
		$st=unserialize($state);
		parent::loadStateRecursive($st,$needViewState);
	}

	/**
	 * Saves all control state (viewstate and controlstate) as a collection.
	 * This method overrides the parent implementation by saving state
	 * into cache if needed.
	 * This method should only be used by framework developers.
	 * @param boolean whether the viewstate should be saved
	 * @return array the collection of the control state (including its children's state).
	 */
	protected function &saveStateRecursive($needViewState=true)
	{
		if($this->_dataCached)
			return $this->_state;
		else
		{
			$st=parent::saveStateRecursive($needViewState);
			// serialization is needed to avoid undefined classes when loading state
			$this->_state=serialize($st);
			return $this->_state;
		}
	}

	/**
	 * Registers an action associated with the content being cached.
	 * The registered action will be replayed if the content stored
	 * in the cache is served to end-users.
	 * @param string context of the action method. This is a property-path
	 * referring to the context object (e.g. Page, Page.ClientScript)
	 * @param string method name of the context object
	 * @param array list of parameters to be passed to the action method
	 */
	public function registerAction($context,$funcName,$funcParams)
	{
		$this->_actions[]=array($context,$funcName,$funcParams);
	}

	private function getCacheKey()
	{
		if($this->_cacheKey===null)
			$this->_cacheKey=$this->calculateCacheKey();
		return $this->_cacheKey;
	}

	/**
	 * Calculates the cache key.
	 * The key is calculated based on the unique ID of this control
	 * and the request parameters specified via {@link setVaryByParam VaryByParam}.
	 * If {@link getVaryBySession VaryBySession} is true, the session ID
	 * will also participate in the key calculation.
	 * This method may be overriden to support other variations in
	 * the calculated cache key.
	 * @return string cache key
	 */
	protected function calculateCacheKey()
	{
		$key=$this->getBaseCacheKey();
		if($this->_varyBySession)
			$key.=$this->getSession()->getSessionID();
		if($this->_varyByParam!=='')
		{
			$params=array();
			$request=$this->getRequest();
			foreach(explode(',',$this->_varyByParam) as $name)
			{
				$name=trim($name);
				$params[$name]=$request->itemAt($name);
			}
			$key.=serialize($params);
		}
		$param=new TOutputCacheCalculateKeyEventParameter;
		$this->onCalculateKey($param);
		$key.=$param->getCacheKey();
		return $key;
	}

	/**
	 * @return string basic cache key without variations
	 */
	protected function getBaseCacheKey()
	{
		return self::CACHE_ID_PREFIX.$this->_keyPrefix.$this->getPage()->getPagePath().$this->getUniqueID();
	}

	/**
	 * @return string the ID of the cache module. Defaults to '', meaning the primary cache module is used.
	 */
	public function getCacheModuleID()
	{
		return $this->_cacheModuleID;
	}

	/**
	 * @param string the ID of the cache module. If empty, the primary cache module will be used.
	 */
	public function setCacheModuleID($value)
	{
		$this->_cacheModuleID=$value;
	}

	/**
	 * Sets the prefix of the cache key.
	 * This method is used internally by {@link TTemplate}.
	 * @param string key prefix
	 */
	public function setCacheKeyPrefix($value)
	{
		$this->_keyPrefix=$value;
	}

	/**
	 * @return integer the timestamp of the cached content. This is only valid if the content is being cached.
	 * @since 3.1.1
	 */
	public function getCacheTime()
	{
		return $this->_cacheTime;
	}

	/**
	 * Returns the dependency of the data to be cached.
	 * The default implementation simply returns null, meaning no specific dependency.
	 * This method may be overriden to associate the data to be cached
	 * with additional dependencies.
	 * @return ICacheDependency
	 */
	protected function getCacheDependency()
	{
		return null;
	}

	/**
	 * @return boolean whether content enclosed is cached or not
	 */
	public function getContentCached()
	{
		return $this->_dataCached;
	}

	/**
	 * @return integer number of seconds that the data can remain in cache. Defaults to 60 seconds.
	 * Note, if cache dependency changes or cache space is limited,
	 * the data may be purged out of cache earlier.
	 */
	public function getDuration()
	{
		return $this->_duration;
	}

	/**
	 * @param integer number of seconds that the data can remain in cache. If 0, it means data is not cached.
	 * @throws TInvalidDataValueException if the value is smaller than 0.
	 */
	public function setDuration($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			throw new TInvalidDataValueException('outputcache_duration_invalid',get_class($this));
		$this->_duration=$value;
	}

	/**
	 * @return string a semicolon-separated list of strings used to vary the output cache. Defaults to ''.
	 */
	public function getVaryByParam()
	{
		return $this->_varyByParam;
	}

	/**
	 * Sets the names of the request parameters that should be used in calculating the cache key.
	 * The names should be concatenated by semicolons.
	 * By setting this value, the output cache will use different cached data
	 * for each different set of request parameter values.
	 * @return string a semicolon-separated list of strings used to vary the output cache.
	 */
	public function setVaryByParam($value)
	{
		$this->_varyByParam=trim($value);
	}

	/**
	 * @return boolean whether the content being cached should be differentiated according to user sessions. Defaults to false.
	 */
	public function getVaryBySession()
	{
		return $this->_varyBySession;
	}

	/**
	 * @param boolean whether the content being cached should be differentiated according to user sessions.
	 */
	public function setVaryBySession($value)
	{
		$this->_varyBySession=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return boolean whether cached output will be used on postback requests. Defaults to false.
	 */
	public function getCachingPostBack()
	{
		return $this->_cachePostBack;
	}

	/**
	 * Sets a value indicating whether cached output will be used on postback requests.
	 * By default, this is disabled. Be very cautious when enabling it.
	 * If the cached content including interactive user controls such as
	 * TTextBox, TDropDownList, your page may fail to render on postbacks.
	 * @param boolean whether cached output will be used on postback requests.
	 */
	public function setCachingPostBack($value)
	{
		$this->_cachePostBack=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * This event is raised when the output cache is checking cache dependency.
	 * An event handler may be written to check customized dependency conditions.
	 * The checking result should be saved by setting {@link TOutputCacheCheckDependencyEventParameter::setIsValid IsValid}
	 * property of the event parameter (which defaults to true).
	 * @param TOutputCacheCheckDependencyEventParameter event parameter
	 */
	public function onCheckDependency($param)
	{
		$this->raiseEvent('OnCheckDependency',$this,$param);
	}

	/**
	 * This event is raised when the output cache is calculating cache key.
	 * By varying cache keys, one can obtain different versions of cached content.
	 * An event handler may be written to add variety of the key calculation.
	 * The value set in {@link TOutputCacheCalculateKeyEventParameter::setCacheKey CacheKey} of
	 * this event parameter will be appended to the default key calculation scheme.
	 * @param TOutputCacheCalculateKeyEventParameter event parameter
	 */
	public function onCalculateKey($param)
	{
		$this->raiseEvent('OnCalculateKey',$this,$param);
	}

	/**
	 * Renders the output cache control.
	 * This method overrides the parent implementation by capturing the output
	 * from its child controls and saving it into cache, if output cache is needed.
	 * @param THtmlWriter
	 */
	public function render($writer)
	{
		if($this->_dataCached)
			$writer->write($this->_contents);
		else if($this->_cacheAvailable)
		{
			$htmlWriter = Prado::createComponent($this->GetResponse()->getHtmlWriterType(), new TTextWriter());
			
			$stack=$this->getPage()->getCachingStack();
			$stack->push($this);
			parent::render($htmlWriter);
			$stack->pop();

			$content=$htmlWriter->flush();
			$data=array($content,$this->_state,$this->_actions,time());
			$this->_cache->set($this->getCacheKey(),$data,$this->getDuration(),$this->getCacheDependency());
			$writer->write($content);
		}
		else
			parent::render($writer);
	}
}

/**
 * TOutputCacheCheckDependencyEventParameter class
 *
 * TOutputCacheCheckDependencyEventParameter encapsulates the parameter data for
 * <b>OnCheckDependency</b> event of {@link TOutputCache} control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TOutputCache.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TOutputCacheCheckDependencyEventParameter extends TEventParameter
{
	private $_isValid=true;
	private $_cacheTime=0;

	/**
	 * @return boolean whether the dependency remains valid. Defaults to true.
	 */
	public function getIsValid()
	{
		return $this->_isValid;
	}

	/**
	 * @param boolean whether the dependency remains valid
	 */
	public function setIsValid($value)
	{
		$this->_isValid=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return integer the timestamp of the cached result. You may use this to help determine any dependency is changed.
	 * @since 3.1.1
	 */
	public function getCacheTime()
	{
		return $this->_cacheTime;
	}

	/**
	 * @param integer the timestamp of the cached result. This is used internally.
	 * @since 3.1.1
	 */
	public function setCacheTime($value)
	{
		$this->_cacheTime=TPropertyValue::ensureInteger($value);
	}
}


/**
 * TOutputCacheCalculateKeyEventParameter class
 *
 * TOutputCacheCalculateKeyEventParameter encapsulates the parameter data for
 * <b>OnCalculateKey</b> event of {@link TOutputCache} control.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TOutputCache.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TOutputCacheCalculateKeyEventParameter extends TEventParameter
{
	/**
	 * @var string cache key to be appended to the default calculation scheme.
	 */
	private $_cacheKey='';

	/**
	 * @return string cache key to be appended to the default calculation scheme.
	 */
	public function getCacheKey()
	{
		return $this->_cacheKey;
	}

	/**
	 * @param string cache key to be appended to the default calculation scheme
	 */
	public function setCacheKey($value)
	{
		$this->_cacheKey=TPropertyValue::ensureString($value);
	}
}

