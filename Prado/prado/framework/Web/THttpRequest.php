<?php
/**
 * THttpRequest, THttpCookie, THttpCookieCollection, TUri class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: THttpRequest.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web
 */

Prado::using('System.Web.TUrlManager');

/**
 * THttpRequest class
 *
 * THttpRequest provides storage and access scheme for user request sent via HTTP.
 * It also encapsulates a uniform way to parse and construct URLs.
 *
 * User post data can be retrieved from THttpRequest by using it like an associative array.
 * For example, to test if a user supplies a variable named 'param1', you can use,
 * <code>
 *   if(isset($request['param1'])) ...
 *   // equivalent to:
 *   // if($request->contains('param1')) ...
 * </code>
 * To get the value of 'param1', use,
 * <code>
 *   $value=$request['param1'];
 *   // equivalent to:
 *   //   $value=$request->itemAt('param1');
 * </code>
 * To traverse the user post data, use
 * <code>
 *   foreach($request as $name=>$value) ...
 * </code>
 * Note, POST and GET variables are merged together in THttpRequest.
 * If a variable name appears in both POST and GET data, then POST data
 * takes precedence.
 *
 * To construct a URL that can be recognized by Prado, use {@link constructUrl()}.
 * The format of the recognizable URLs is determined according to
 * {@link setUrlManager UrlManager}. By default, the following two formats
 * are recognized:
 * <code>
 * /index.php?ServiceID=ServiceParameter&Name1=Value1&Name2=Value2
 * /index.php/ServiceID,ServiceParameter/Name1,Value1/Name2,Value2
 * </code>
 * The first format is called 'Get' while the second 'Path', which is specified
 * via {@link setUrlFormat UrlFormat}. For advanced users who want to use
 * their own URL formats, they can write customized URL management modules
 * and install the managers as application modules and set {@link setUrlManager UrlManager}.
 *
 * The ServiceID in the above URLs is as defined in the application configuration
 * (e.g. the default page service's service ID is 'page').
 * As a consequence, your GET variable names should not conflict with the service
 * IDs that your application supports.
 *
 * THttpRequest also provides the cookies sent by the user, user information such
 * as his browser capabilities, accepted languages, etc.
 *
 * By default, THttpRequest is registered with {@link TApplication} as the
 * request module. It can be accessed via {@link TApplication::getRequest()}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpRequest.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web
 * @since 3.0
 */
class THttpRequest extends TApplicationComponent implements IteratorAggregate,ArrayAccess,Countable,IModule
{
	const CGIFIX__PATH_INFO		= 1;
	const CGIFIX__SCRIPT_NAME	= 2;
	/**
	 * @var TUrlManager the URL manager module
	 */
	private $_urlManager=null;
	/**
	 * @var string the ID of the URL manager module
	 */
	private $_urlManagerID='';
	/**
	 * @var string Separator used to separate GET variable name and value when URL format is Path.
	 */
	private $_separator=',';
	/**
	 * @var string requested service ID
	 */
	private $_serviceID=null;
	/**
	 * @var string requested service parameter
	 */
	private $_serviceParam=null;
	/**
	 * @var THttpCookieCollection cookies sent from user
	 */
	private $_cookies=null;
	/**
	 * @var string requested URI (URL w/o host info)
	 */
	private $_requestUri;
	/**
	 * @var string path info of URL
	 */
	private $_pathInfo;
	/**
	 * @var boolean whether the session ID should be kept in cookie only
	 */
	private $_cookieOnly=null;
	private $_urlFormat=THttpRequestUrlFormat::Get;
	private $_services;
	private $_requestResolved=false;
	private $_enableCookieValidation=false;
	private $_cgiFix=0;
	/**
	 * @var string request URL
	 */
	private $_url=null;

	/**
	 * @var string module id
	 */
	private $_id;

	/**
	 * @var array contains all request variables
	 */
	private $_items=array();

	/**
	 * @return string id of this module
	 */
	public function getID()
	{
		return $this->_id;
	}

	/**
	 * @param string id of this module
	 */
	public function setID($value)
	{
		$this->_id=$value;
	}

	/**
	 * Initializes the module.
	 * This method is required by IModule and is invoked by application.
	 * @param TXmlElement module configuration
	 */
	public function init($config)
	{
		if(empty($this->_urlManagerID))
		{
			$this->_urlManager=new TUrlManager;
			$this->_urlManager->init(null);
		}
		else
		{
			$this->_urlManager=$this->getApplication()->getModule($this->_urlManagerID);
			if($this->_urlManager===null)
				throw new TConfigurationException('httprequest_urlmanager_inexist',$this->_urlManagerID);
			if(!($this->_urlManager instanceof TUrlManager))
				throw new TConfigurationException('httprequest_urlmanager_invalid',$this->_urlManagerID);
		}

		// Fill in default request info when the script is run in command line
		if(php_sapi_name()==='cli')
		{
			$_SERVER['REMOTE_ADDR']='127.0.0.1';
			$_SERVER['REQUEST_METHOD']='GET';
			$_SERVER['SERVER_NAME']='localhost';
			$_SERVER['SERVER_PORT']=80;
			$_SERVER['HTTP_USER_AGENT']='';
		}

		// Info about server variables:
		// PHP_SELF contains real URI (w/ path info, w/o query string)
		// SCRIPT_NAME is the real URI for the requested script (w/o path info and query string)
		// QUERY_STRING is the string following the '?' in the ur (eg the a=x part in http://foo/bar?a=x)
		// REQUEST_URI contains the URI part entered in the browser address bar
		// SCRIPT_FILENAME is the file path to the executing script
		if(isset($_SERVER['REQUEST_URI']))
			$this->_requestUri=$_SERVER['REQUEST_URI'];
		else  // TBD: in this case, SCRIPT_NAME need to be escaped
			$this->_requestUri=$_SERVER['SCRIPT_NAME'].(empty($_SERVER['QUERY_STRING'])?'':'?'.$_SERVER['QUERY_STRING']);

		if($this->_cgiFix&self::CGIFIX__PATH_INFO && isset($_SERVER['ORIG_PATH_INFO']))
			$this->_pathInfo=substr($_SERVER['ORIG_PATH_INFO'], strlen($_SERVER['SCRIPT_NAME']));
		elseif(isset($_SERVER['PATH_INFO']))
			$this->_pathInfo=$_SERVER['PATH_INFO'];
		else if(strpos($_SERVER['PHP_SELF'],$_SERVER['SCRIPT_NAME'])===0 && $_SERVER['PHP_SELF']!==$_SERVER['SCRIPT_NAME'])
			$this->_pathInfo=substr($_SERVER['PHP_SELF'],strlen($_SERVER['SCRIPT_NAME']));
		else
			$this->_pathInfo='';

		if(get_magic_quotes_gpc())
		{
			if(isset($_GET))
				$_GET=$this->stripSlashes($_GET);
			if(isset($_POST))
				$_POST=$this->stripSlashes($_POST);
			if(isset($_REQUEST))
				$_REQUEST=$this->stripSlashes($_REQUEST);
			if(isset($_COOKIE))
				$_COOKIE=$this->stripSlashes($_COOKIE);
		}

		$this->getApplication()->setRequest($this);
	}

	/**
	 * Strips slashes from input data.
	 * This method is applied when magic quotes is enabled.
	 * @param mixed input data to be processed
	 * @return mixed processed data
	 */
	public function stripSlashes(&$data)
	{
		return is_array($data)?array_map(array($this,'stripSlashes'),$data):stripslashes($data);
	}

	/**
	 * @return TUri the request URL
	 */
	public function getUrl()
	{
		if($this->_url===null)
		{
			$secure=$this->getIsSecureConnection();
			$url=$secure?'https://':'http://';
			if(empty($_SERVER['HTTP_HOST']))
			{
				$url.=$_SERVER['SERVER_NAME'];
				$port=$_SERVER['SERVER_PORT'];
				if(($port!=80 && !$secure) || ($port!=443 && $secure))
					$url.=':'.$port;
			}
			else
				$url.=$_SERVER['HTTP_HOST'];
			$url.=$this->getRequestUri();
			$this->_url=new TUri($url);
		}
		return $this->_url;
	}

	/**
	 * @return string the ID of the URL manager module
	 */
	public function getUrlManager()
	{
		return $this->_urlManagerID;
	}

	/**
	 * Sets the URL manager module.
	 * By default, {@link TUrlManager} is used for managing URLs.
	 * You may specify a different module for URL managing tasks
	 * by loading it as an application module and setting this property
	 * with the module ID.
	 * @param string the ID of the URL manager module
	 */
	public function setUrlManager($value)
	{
		$this->_urlManagerID=$value;
	}

	/**
	 * @return TUrlManager the URL manager module
	 */
	public function getUrlManagerModule()
	{
		return $this->_urlManager;
	}

	/**
	 * @return THttpRequestUrlFormat the format of URLs. Defaults to THttpRequestUrlFormat::Get.
	 */
	public function getUrlFormat()
	{
		return $this->_urlFormat;
	}

	/**
	 * Sets the format of URLs constructed and interpretted by the request module.
	 * A Get URL format is like index.php?name1=value1&name2=value2
	 * while a Path URL format is like index.php/name1,value1/name2,value.
	 * Changing the UrlFormat will affect {@link constructUrl} and how GET variables
	 * are parsed.
	 * @param THttpRequestUrlFormat the format of URLs.
	 */
	public function setUrlFormat($value)
	{
		$this->_urlFormat=TPropertyValue::ensureEnum($value,'THttpRequestUrlFormat');
	}

	/**
	 * @return string separator used to separate GET variable name and value when URL format is Path. Defaults to comma ','.
	 */
	public function getUrlParamSeparator()
	{
		return $this->_separator;
	}

	/**
	 * @param string separator used to separate GET variable name and value when URL format is Path.
	 * @throws TInvalidDataValueException if the separator is not a single character
	 */
	public function setUrlParamSeparator($value)
	{
		if(strlen($value)===1)
			$this->_separator=$value;
		else
			throw new TInvalidDataValueException('httprequest_separator_invalid');
	}

	/**
	 * @return string request type, can be GET, POST, HEAD, or PUT
	 */
	public function getRequestType()
	{
		return $_SERVER['REQUEST_METHOD'];
	}

	/**
	 * @return boolean if the request is sent via secure channel (https)
	 */
	public function getIsSecureConnection()
	{
	    return isset($_SERVER['HTTPS']) && strcasecmp($_SERVER['HTTPS'],'off');
	}

	/**
	 * @return string part of the request URL after script name and before question mark.
	 */
	public function getPathInfo()
	{
		return $this->_pathInfo;
	}

	/**
	 * @return string part of that request URL after the question mark
	 */
	public function getQueryString()
	{
		return isset($_SERVER['QUERY_STRING'])?$_SERVER['QUERY_STRING']:'';
	}

	/**
	 * @return string the requested http procolol. Blank string if not defined.
	 */
	public function getHttpProtocolVersion ()
	{
		return isset($_SERVER['SERVER_PROTOCOL'])?$_SERVER['SERVER_PROTOCOL']:'';
	}

	/**
	 * @return string part of that request URL after the host info (including pathinfo and query string)
	 */
	public function getRequestUri()
	{
		return $this->_requestUri;
	}

	/**
	 * @param boolean whether to use HTTPS instead of HTTP even if the current request is sent via HTTP
	 * @return string schema and hostname of the requested URL
	 */
	public function getBaseUrl($forceSecureConnection=false)
	{
		$url=$this->getUrl();
		$scheme=($forceSecureConnection)?"https":$url->getScheme();
		$host=$url->getHost();
		if (($port=$url->getPort())) $host.=':'.$port;
		return $scheme.'://'.$host;
	}

	/**
	 * @return string entry script URL (w/o host part)
	 */
	public function getApplicationUrl()
	{
		if($this->_cgiFix&self::CGIFIX__SCRIPT_NAME && isset($_SERVER['ORIG_SCRIPT_NAME']))
			return $_SERVER['ORIG_SCRIPT_NAME'];

		return $_SERVER['SCRIPT_NAME'];
	}

	/**
	 * @param boolean whether to use HTTPS instead of HTTP even if the current request is sent via HTTP
	 * @return string entry script URL (w/ host part)
	 */
	public function getAbsoluteApplicationUrl($forceSecureConnection=false)
	{
		return $this->getBaseUrl($forceSecureConnection) . $this->getApplicationUrl();
	}

	/**
	 * @return string application entry script file path (processed w/ realpath())
	 */
	public function getApplicationFilePath()
	{
		return realpath($_SERVER['SCRIPT_FILENAME']);
	}

	/**
	 * @return string server name
	 */
	public function getServerName()
	{
		return $_SERVER['SERVER_NAME'];
	}

	/**
	 * @return integer server port number
	 */
	public function getServerPort()
	{
		return $_SERVER['SERVER_PORT'];
	}

	/**
	 * @return string URL referrer, null if not present
	 */
	public function getUrlReferrer()
	{
		return isset($_SERVER['HTTP_REFERER'])?$_SERVER['HTTP_REFERER']:null;
	}

	/**
	 * @return array user browser capabilities
	 * @see get_browser
	 */
	public function getBrowser()
	{
	    try
	    {
		    return get_browser();
	    }
	    catch(TPhpErrorException $e)
	    {
	        throw new TConfigurationException('httprequest_browscap_required');
	    }
	}

	/**
	 * @return string user agent
	 */
	public function getUserAgent()
	{
		return $_SERVER['HTTP_USER_AGENT'];
	}

	/**
	 * @return string user IP address
	 */
	public function getUserHostAddress()
	{
		return $_SERVER['REMOTE_ADDR'];
	}

	/**
	 * @return string user host name, null if cannot be determined
	 */
	public function getUserHost()
	{
		return isset($_SERVER['REMOTE_HOST'])?$_SERVER['REMOTE_HOST']:null;
	}

	/**
	 * @return string user browser accept types
	 */
	public function getAcceptTypes()
	{
		// TBD: break it into array??
		return $_SERVER['HTTP_ACCEPT'];
	}

	/**
	 * Returns a list of user preferred languages.
	 * The languages are returned as an array. Each array element
	 * represents a single language preference. The languages are ordered
	 * according to user preferences. The first language is the most preferred.
	 * @return array list of user preferred languages.
	 */
	public function getUserLanguages()
	{
		return Prado::getUserLanguages();
	}

	/**
	 * @return boolean whether cookies should be validated. Defaults to false.
	 */
	public function getEnableCookieValidation()
	{
		return $this->_enableCookieValidation;
	}

	/**
	 * @param boolean whether cookies should be validated.
	 */
	public function setEnableCookieValidation($value)
	{
		$this->_enableCookieValidation=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return integer whether to use ORIG_PATH_INFO and/or ORIG_SCRIPT_NAME. Defaults to 0.
	 * @see THttpRequest::CGIFIX__PATH_INFO, THttpRequest::CGIFIX__SCRIPT_NAME
	 */
	public function getCgiFix()
	{
		return $this->_cgiFix;
	}

	/**
	 * Enable this, if you're using PHP via CGI with php.ini setting "cgi.fix_pathinfo=1"
	 * and have trouble with friendly URL feature. Enable this only if you really know what you are doing!
	 * @param integer enable bitwise to use ORIG_PATH_INFO and/or ORIG_SCRIPT_NAME.
	 * @see THttpRequest::CGIFIX__PATH_INFO, THttpRequest::CGIFIX__SCRIPT_NAME
	 */
	public function setCgiFix($value)
	{
		$this->_cgiFix=TPropertyValue::ensureInteger($value);
	}

	/**
	 * @return THttpCookieCollection list of cookies to be sent
	 */
	public function getCookies()
	{
		if($this->_cookies===null)
		{
			$this->_cookies=new THttpCookieCollection;
			if($this->getEnableCookieValidation())
			{
				$sm=$this->getApplication()->getSecurityManager();
				foreach($_COOKIE as $key=>$value)
				{
					if(($value=$sm->validateData($value))!==false)
						$this->_cookies->add(new THttpCookie($key,$value));
				}
			}
			else
			{
				foreach($_COOKIE as $key=>$value)
					$this->_cookies->add(new THttpCookie($key,$value));
			}
		}
		return $this->_cookies;
	}

	/**
	 * @return array list of uploaded files.
	 */
	public function getUploadedFiles()
	{
		return $_FILES;
	}

	/**
	 * @return array list of server variables.
	 */
	public function getServerVariables()
	{
		return $_SERVER;
	}

	/**
	 * @return array list of environment variables.
	 */
	public function getEnvironmentVariables()
	{
		return $_ENV;
	}

	/**
	 * Constructs a URL that can be recognized by PRADO.
	 * The actual construction work is done by the URL manager module.
	 * This method may append session information to the generated URL if needed.
	 * You may provide your own URL manager module by setting {@link setUrlManager UrlManager}
	 * to provide your own URL scheme.
	 *
	 * Note, the constructed URL does not contain the protocol and hostname part.
	 * You may obtain an absolute URL by prepending the constructed URL with {@link getBaseUrl BaseUrl}.
	 * @param string service ID
	 * @param string service parameter
	 * @param array GET parameters, null if not needed
	 * @param boolean whether to encode the ampersand in URL, defaults to true.
	 * @param boolean whether to encode the GET parameters (their names and values), defaults to false.
	 * @return string URL
	 * @see TUrlManager::constructUrl
	 */
	public function constructUrl($serviceID,$serviceParam,$getItems=null,$encodeAmpersand=true,$encodeGetItems=true)
	{
		if ($this->_cookieOnly===null)
				$this->_cookieOnly=(int)ini_get('session.use_cookies') && (int)ini_get('session.use_only_cookies');
		$url=$this->_urlManager->constructUrl($serviceID,$serviceParam,$getItems,$encodeAmpersand,$encodeGetItems);
		if(defined('SID') && SID != '' && !$this->_cookieOnly)
			return $url . (strpos($url,'?')===false? '?' : ($encodeAmpersand?'&amp;':'&')) . SID;
		else
			return $url;
	}

	/**
	 * Parses the request URL and returns an array of input parameters (excluding GET variables).
	 * You may override this method to support customized URL format.
	 * @return array list of input parameters, indexed by parameter names
	 * @see TUrlManager::parseUrl
	 */
	protected function parseUrl()
	{
		return $this->_urlManager->parseUrl();
	}

	/**
	 * Resolves the requested service.
	 * This method implements a URL-based service resolution.
	 * A URL in the format of /index.php?sp=serviceID.serviceParameter
	 * will be resolved with the serviceID and the serviceParameter.
	 * You may override this method to provide your own way of service resolution.
	 * @param array list of valid service IDs
	 * @return string the currently requested service ID, null if no service ID is found
	 * @see constructUrl
	 */
	public function resolveRequest($serviceIDs)
	{
		Prado::trace("Resolving request from ".$_SERVER['REMOTE_ADDR'],'System.Web.THttpRequest');
		$getParams=$this->parseUrl();
		foreach($getParams as $name=>$value)
			$_GET[$name]=$value;
		$this->_items=array_merge($_GET,$_POST);
		$this->_requestResolved=true;
		foreach($serviceIDs as $serviceID)
		{
			if($this->contains($serviceID))
			{
				$this->setServiceID($serviceID);
				$this->setServiceParameter($this->itemAt($serviceID));
				return $serviceID;
			}
		}
		return null;
	}

	/**
	 * @return boolean true if request is already resolved, false otherwise.
	 */
	public function getRequestResolved()
	{
		return $this->_requestResolved;
	}

	/**
	 * @return string requested service ID
	 */
	public function getServiceID()
	{
		return $this->_serviceID;
	}

	/**
	 * Sets the requested service ID.
	 * @param string requested service ID
	 */
	public function setServiceID($value)
	{
		$this->_serviceID=$value;
	}

	/**
	 * @return string requested service parameter
	 */
	public function getServiceParameter()
	{
		return $this->_serviceParam;
	}

	/**
	 * Sets the requested service parameter.
	 * @param string requested service parameter
	 */
	public function setServiceParameter($value)
	{
		$this->_serviceParam=$value;
	}

	//------ The following methods enable THttpRequest to be TMap-like -----

	/**
	 * Returns an iterator for traversing the items in the list.
	 * This method is required by the interface IteratorAggregate.
	 * @return Iterator an iterator for traversing the items in the list.
	 */
	public function getIterator()
	{
		return new TMapIterator($this->_items);
	}

	/**
	 * @return integer the number of items in the request
	 */
	public function getCount()
	{
		return count($this->_items);
	}

	/**
	 * Returns the number of items in the request.
	 * This method is required by Countable interface.
	 * @return integer number of items in the request.
	 */
	public function count()
	{
		return $this->getCount();
	}

	/**
	 * @return array the key list
	 */
	public function getKeys()
	{
		return array_keys($this->_items);
	}

	/**
	 * Returns the item with the specified key.
	 * This method is exactly the same as {@link offsetGet}.
	 * @param mixed the key
	 * @return mixed the element at the offset, null if no element is found at the offset
	 */
	public function itemAt($key)
	{
		return isset($this->_items[$key]) ? $this->_items[$key] : null;
	}

	/**
	 * Adds an item into the request.
	 * Note, if the specified key already exists, the old value will be overwritten.
	 * @param mixed key
	 * @param mixed value
	 */
	public function add($key,$value)
	{
		$this->_items[$key]=$value;
	}

	/**
	 * Removes an item from the request by its key.
	 * @param mixed the key of the item to be removed
	 * @return mixed the removed value, null if no such key exists.
	 * @throws TInvalidOperationException if the item cannot be removed
	 */
	public function remove($key)
	{
		if(isset($this->_items[$key]) || array_key_exists($key,$this->_items))
		{
			$value=$this->_items[$key];
			unset($this->_items[$key]);
			return $value;
		}
		else
			return null;
	}

	/**
	 * Removes all items in the request.
	 */
	public function clear()
	{
		foreach(array_keys($this->_items) as $key)
			$this->remove($key);
	}

	/**
	 * @param mixed the key
	 * @return boolean whether the request contains an item with the specified key
	 */
	public function contains($key)
	{
		return isset($this->_items[$key]) || array_key_exists($key,$this->_items);
	}

	/**
	 * @return array the list of items in array
	 */
	public function toArray()
	{
		return $this->_items;
	}

	/**
	 * Returns whether there is an element at the specified offset.
	 * This method is required by the interface ArrayAccess.
	 * @param mixed the offset to check on
	 * @return boolean
	 */
	public function offsetExists($offset)
	{
		return $this->contains($offset);
	}

	/**
	 * Returns the element at the specified offset.
	 * This method is required by the interface ArrayAccess.
	 * @param integer the offset to retrieve element.
	 * @return mixed the element at the offset, null if no element is found at the offset
	 */
	public function offsetGet($offset)
	{
		return $this->itemAt($offset);
	}

	/**
	 * Sets the element at the specified offset.
	 * This method is required by the interface ArrayAccess.
	 * @param integer the offset to set element
	 * @param mixed the element value
	 */
	public function offsetSet($offset,$item)
	{
		$this->add($offset,$item);
	}

	/**
	 * Unsets the element at the specified offset.
	 * This method is required by the interface ArrayAccess.
	 * @param mixed the offset to unset element
	 */
	public function offsetUnset($offset)
	{
		$this->remove($offset);
	}
}

/**
 * THttpCookieCollection class.
 *
 * THttpCookieCollection implements a collection class to store cookies.
 * Besides using all functionalities from {@link TList}, you can also
 * retrieve a cookie by its name using either {@link findCookieByName} or
 * simply:
 * <code>
 *   $cookie=$collection[$cookieName];
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpRequest.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web
 * @since 3.0
 */
class THttpCookieCollection extends TList
{
	/**
	 * @var mixed owner of this collection
	 */
	private $_o;

	/**
	 * Constructor.
	 * @param mixed owner of this collection.
	 */
	public function __construct($owner=null)
	{
		$this->_o=$owner;
	}

	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by performing additional
	 * operations for each newly added THttpCookie object.
	 * @param integer the specified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a THttpCookie object.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof THttpCookie)
		{
			parent::insertAt($index,$item);
			if($this->_o instanceof THttpResponse)
				$this->_o->addCookie($item);
		}
		else
			throw new TInvalidDataTypeException('httpcookiecollection_httpcookie_required');
	}

	/**
	 * Removes an item at the specified position.
	 * This overrides the parent implementation by performing additional
	 * cleanup work when removing a TCookie object.
	 * @param integer the index of the item to be removed.
	 * @return mixed the removed item.
	 */
	public function removeAt($index)
	{
		$item=parent::removeAt($index);
		if($this->_o instanceof THttpResponse)
			$this->_o->removeCookie($item);
		return $item;
	}

	/**
	 * @param integer|string index of the cookie in the collection or the cookie's name
	 * @return THttpCookie the cookie found
	 */
	public function itemAt($index)
	{
		if(is_integer($index))
			return parent::itemAt($index);
		else
			return $this->findCookieByName($index);
	}

	/**
	 * Finds the cookie with the specified name.
	 * @param string the name of the cookie to be looked for
	 * @return THttpCookie the cookie, null if not found
	 */
	public function findCookieByName($name)
	{
		foreach($this as $cookie)
			if($cookie->getName()===$name)
				return $cookie;
		return null;
	}
}

/**
 * THttpCookie class.
 *
 * A THttpCookie instance stores a single cookie, including the cookie name, value,
 * domain, path, expire, and secure.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpRequest.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web
 * @since 3.0
 */
class THttpCookie extends TComponent
{
	/**
	 * @var string domain of the cookie
	 */
	private $_domain='';
	/**
	 * @var string name of the cookie
	 */
	private $_name;
	/**
	 * @var string value of the cookie
	 */
	private $_value='';
	/**
	 * @var integer expire of the cookie
	 */
	private $_expire=0;
	/**
	 * @var string path of the cookie
	 */
	private $_path='/';
	/**
	 * @var boolean whether cookie should be sent via secure connection
	 */
	private $_secure=false;
	/**
	 * @var boolean if true the cookie value will be unavailable to JavaScript
	 */
	private $_httpOnly=false;

	/**
	 * Constructor.
	 * @param string name of this cookie
	 * @param string value of this cookie
	 */
	public function __construct($name,$value)
	{
		$this->_name=$name;
		$this->_value=$value;
	}

	/**
	 * @return string the domain to associate the cookie with
	 */
	public function getDomain()
	{
		return $this->_domain;
	}

	/**
	 * @param string the domain to associate the cookie with
	 */
	public function setDomain($value)
	{
		$this->_domain=$value;
	}

	/**
	 * @return integer the time the cookie expires. This is a Unix timestamp so is in number of seconds since the epoch.
	 */
	public function getExpire()
	{
		return $this->_expire;
	}

	/**
	 * @param integer the time the cookie expires. This is a Unix timestamp so is in number of seconds since the epoch.
	 */
	public function setExpire($value)
	{
		$this->_expire=TPropertyValue::ensureInteger($value);
	}

	/**
	 * @return boolean if true the cookie value will be unavailable to JavaScript
	 */
	public function getHttpOnly()
	{
		return $this->_httpOnly;
	}

	/**
	 * @param boolean $value if true the cookie value will be unavailable to JavaScript
	 */
	public function setHttpOnly($value)
	{
		$this->_httpOnly = TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return string the name of the cookie
	 */
	public function getName()
	{
		return $this->_name;
	}

	/**
	 * @param string the name of the cookie
	 */
	public function setName($value)
	{
		$this->_name=$value;
	}

	/**
	 * @return string the value of the cookie
	 */
	public function getValue()
	{
		return $this->_value;
	}

	/**
	 * @param string the value of the cookie
	 */
	public function setValue($value)
	{
		$this->_value=$value;
	}

	/**
	 * @return string the path on the server in which the cookie will be available on, default is '/'
	 */
	public function getPath()
	{
		return $this->_path;
	}

	/**
	 * @param string the path on the server in which the cookie will be available on
	 */
	public function setPath($value)
	{
		$this->_path=$value;
	}

	/**
	 * @return boolean whether the cookie should only be transmitted over a secure HTTPS connection
	 */
	public function getSecure()
	{
		return $this->_secure;
	}

	/**
	 * @param boolean ether the cookie should only be transmitted over a secure HTTPS connection
	 */
	public function setSecure($value)
	{
		$this->_secure=TPropertyValue::ensureBoolean($value);
	}
}

/**
 * TUri class
 *
 * TUri represents a URI. Given a URI
 * http://joe:whatever@example.com:8080/path/to/script.php?param=value#anchor
 * it will be decomposed as follows,
 * - scheme: http
 * - host: example.com
 * - port: 8080
 * - user: joe
 * - password: whatever
 * - path: /path/to/script.php
 * - query: param=value
 * - fragment: anchor
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpRequest.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web
 * @since 3.0
 */
class TUri extends TComponent
{
	/**
	 * @var array list of default ports for known schemes
	 */
	private static $_defaultPort=array(
		'ftp'=>21,
		'gopher'=>70,
		'http'=>80,
		'https'=>443,
		'news'=>119,
		'nntp'=>119,
		'wais'=>210,
		'telnet'=>23
	);
	/**
	 * @var string scheme of the URI
	 */
	private $_scheme;
	/**
	 * @var string host name of the URI
	 */
	private $_host;
	/**
	 * @var integer port of the URI
	 */
	private $_port;
	/**
	 * @var string user of the URI
	 */
	private $_user;
	/**
	 * @var string password of the URI
	 */
	private $_pass;
	/**
	 * @var string path of the URI
	 */
	private $_path;
	/**
	 * @var string query string of the URI
	 */
	private $_query;
	/**
	 * @var string fragment of the URI
	 */
	private $_fragment;
	/**
	 * @var string the URI
	 */
	private $_uri;

	/**
	 * Constructor.
	 * Decomposes the specified URI into parts.
	 * @param string URI to be represented
	 * @throws TInvalidDataValueException if URI is of bad format
	 */
	public function __construct($uri)
	{
		if(($ret=@parse_url($uri))!==false)
		{
			// decoding???
			$this->_scheme=isset($ret['scheme'])?$ret['scheme']:'';
			$this->_host=isset($ret['host'])?$ret['host']:'';
			$this->_port=isset($ret['port'])?$ret['port']:'';
			$this->_user=isset($ret['user'])?$ret['user']:'';
			$this->_pass=isset($ret['pass'])?$ret['pass']:'';
			$this->_path=isset($ret['path'])?$ret['path']:'';
			$this->_query=isset($ret['query'])?$ret['query']:'';
			$this->_fragment=isset($ret['fragment'])?$ret['fragment']:'';
			$this->_uri=$uri;
		}
		else
		{
			throw new TInvalidDataValueException('uri_format_invalid',$uri);
		}
	}

	/**
	 * @return string URI
	 */
	public function getUri()
	{
		return $this->_uri;
	}

	/**
	 * @return string scheme of the URI, such as 'http', 'https', 'ftp', etc.
	 */
	public function getScheme()
	{
		return $this->_scheme;
	}

	/**
	 * @return string hostname of the URI
	 */
	public function getHost()
	{
		return $this->_host;
	}

	/**
	 * @return integer port number of the URI
	 */
	public function getPort()
	{
		return $this->_port;
	}

	/**
	 * @return string username of the URI
	 */
	public function getUser()
	{
		return $this->_user;
	}

	/**
	 * @return string password of the URI
	 */
	public function getPassword()
	{
		return $this->_pass;
	}

	/**
	 * @return string path of the URI
	 */
	public function getPath()
	{
		return $this->_path;
	}

	/**
	 * @return string query string of the URI
	 */
	public function getQuery()
	{
		return $this->_query;
	}

	/**
	 * @return string fragment of the URI
	 */
	public function getFragment()
	{
		return $this->_fragment;
	}
}

/**
 * THttpRequestUrlFormat class.
 * THttpRequestUrlFormat defines the enumerable type for the possible URL formats
 * that can be recognized by {@link THttpRequest}.
 *
 * The following enumerable values are defined:
 * - Get: the URL format is like /path/to/index.php?name1=value1&name2=value2...
 * - Path: the URL format is like /path/to/index.php/name1,value1/name2,value2...
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpRequest.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web
 * @since 3.0.4
 */
class THttpRequestUrlFormat extends TEnumerable
{
	const Get='Get';
	const Path='Path';
}

