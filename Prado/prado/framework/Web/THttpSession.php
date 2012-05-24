<?php
/**
 * THttpSession class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: THttpSession.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web
 */

/**
 * THttpSession class
 *
 * THttpSession provides session-level data management and the related configurations.
 * To start the session, call {@link open}; to complete and send out session data, call {@link close};
 * to destroy the session, call {@link destroy}. If AutoStart is true, then the session
 * will be started once the session module is loaded and initialized.
 *
 * To access data stored in session, use THttpSession like an associative array. For example,
 * <code>
 *   $session=new THttpSession;
 *   $session->open();
 *   $value1=$session['name1'];  // get session variable 'name1'
 *   $value2=$session['name2'];  // get session variable 'name2'
 *   foreach($session as $name=>$value) // traverse all session variables
 *   $session['name3']=$value3;  // set session variable 'name3'
 * </code>
 *
 * The following configurations are available for session:
 * {@link setAutoStart AutoStart}, {@link setCookieMode CookieMode},
 * {@link setSavePath SavePath},
 * {@link setUseCustomStorage UseCustomStorage}, {@link setGCProbability GCProbability},
 * {@link setTimeout Timeout}.
 * See the corresponding setter and getter documentation for more information.
 * Note, these properties must be set before the session is started.
 *
 * THttpSession can be inherited with customized session storage method.
 * Override {@link _open}, {@link _close}, {@link _read}, {@link _write}, {@link _destroy} and {@link _gc}
 * and set {@link setUseCustomStorage UseCustomStorage} to true.
 * Then, the session data will be stored using the above methods.
 *
 * By default, THttpSession is registered with {@link TApplication} as the
 * request module. It can be accessed via {@link TApplication::getSession()}.
 *
 * THttpSession may be configured in application configuration file as follows,
 * <code>
 * <module id="session" class="THttpSession" SessionName="SSID" SavePath="/tmp"
 *         CookieMode="Allow" UseCustomStorage="false" AutoStart="true" GCProbability="1"
 *         UseTransparentSessionID="true" TimeOut="3600" />
 * </code>
 * where {@link getSessionName SessionName}, {@link getSavePath SavePath},
 * {@link getCookieMode CookieMode}, {@link getUseCustomStorage
 * UseCustomStorage}, {@link getAutoStart AutoStart}, {@link getGCProbability
 * GCProbability}, {@link getUseTransparentSessionID UseTransparentSessionID}
 * and {@link getTimeout TimeOut} are configurable properties of THttpSession.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpSession.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web
 * @since 3.0
 */
class THttpSession extends TApplicationComponent implements IteratorAggregate,ArrayAccess,Countable,IModule
{
	/**
	 * @var boolean whether this module has been initialized
	 */
	private $_initialized=false;
	/**
	 * @var boolean whether the session has started
	 */
	private $_started=false;
	/**
	 * @var boolean whether the session should be started when the module is initialized
	 */
	private $_autoStart=false;
	/**
	 * @var THttpCookie cookie to be used to store session ID and other data
	 */
	private $_cookie=null;
	/**
	 * @var string module id
	 */
	private $_id;
	/**
	 * @var boolean
	 */
	private $_customStorage=false;

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
	 * This method is required by IModule.
	 * If AutoStart is true, the session will be started.
	 * @param TXmlElement module configuration
	 */
	public function init($config)
	{
		if($this->_autoStart)
			$this->open();
		$this->_initialized=true;
		$this->getApplication()->setSession($this);
		register_shutdown_function(array($this, "close"));
	}

	/**
	 * Starts the session if it has not started yet.
	 */
	public function open()
	{
		if(!$this->_started)
		{
			if($this->_customStorage)
				session_set_save_handler(array($this,'_open'),array($this,'_close'),array($this,'_read'),array($this,'_write'),array($this,'_destroy'),array($this,'_gc'));
			if($this->_cookie!==null)
				session_set_cookie_params($this->_cookie->getExpire(),$this->_cookie->getPath(),$this->_cookie->getDomain(),$this->_cookie->getSecure());
			if(ini_get('session.auto_start')!=='1')
				session_start();
			$this->_started=true;
		}
	}

	/**
	 * Ends the current session and store session data.
	 */
	public function close()
	{
		if($this->_started)
		{
			session_write_close();
			$this->_started=false;
		}
	}

	/**
	 * Destroys all data registered to a session.
	 */
	public function destroy()
	{
		if($this->_started)
		{
			session_destroy();
			$this->_started=false;
		}
	}

	/**
	 * @return boolean whether the session has started
	 */
	public function getIsStarted()
	{
		return $this->_started;
	}

	/**
	 * @return string the current session ID
	 */
	public function getSessionID()
	{
		return session_id();
	}

	/**
	 * @param string the session ID for the current session
	 * @throws TInvalidOperationException if session is started already
	 */
	public function setSessionID($value)
	{
		if($this->_started)
			throw new TInvalidOperationException('httpsession_sessionid_unchangeable');
		else
			session_id($value);
	}

	/**
	 * @return string the current session name
	 */
	public function getSessionName()
	{
		return session_name();
	}

	/**
	 * @param string the session name for the current session, must be an alphanumeric string, defaults to PHPSESSID
	 * @throws TInvalidOperationException if session is started already
	 */
	public function setSessionName($value)
	{
		if($this->_started)
			throw new TInvalidOperationException('httpsession_sessionname_unchangeable');
		else if(ctype_alnum($value))
			session_name($value);
		else
			throw new TInvalidDataValueException('httpsession_sessionname_invalid',$value);
	}

	/**
	 * @return string the current session save path, defaults to '/tmp'.
	 */
	public function getSavePath()
	{
		return session_save_path();
	}

	/**
	 * @param string the current session save path
	 * @throws TInvalidOperationException if session is started already
	 */
	public function setSavePath($value)
	{
		if($this->_started)
			throw new TInvalidOperationException('httpsession_savepath_unchangeable');
		else if(is_dir($value))
			session_save_path($value);
		else
			throw new TInvalidDataValueException('httpsession_savepath_invalid',$value);
	}

	/**
	 * @return boolean whether to use user-specified handlers to store session data. Defaults to false.
	 */
	public function getUseCustomStorage()
	{
		return $this->_customStorage;
	}

	/**
	 * @param boolean whether to use user-specified handlers to store session data.
	 * If true, make sure the methods {@link _open}, {@link _close}, {@link _read},
	 * {@link _write}, {@link _destroy}, and {@link _gc} are overridden in child
	 * class, because they will be used as the callback handlers.
	 */
	public function setUseCustomStorage($value)
	{
		$this->_customStorage=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return THttpCookie cookie that will be used to store session ID
	 */
	public function getCookie()
	{
		if($this->_cookie===null)
			$this->_cookie=new THttpCookie($this->getSessionName(),$this->getSessionID());
		return $this->_cookie;
	}

	/**
	 * @return THttpSessionCookieMode how to use cookie to store session ID. Defaults to THttpSessionCookieMode::Allow.
	 */
	public function getCookieMode()
	{
		if(ini_get('session.use_cookies')==='0')
			return THttpSessionCookieMode::None;
		else if(ini_get('session.use_only_cookies')==='0')
			return THttpSessionCookieMode::Allow;
		else
			return THttpSessionCookieMode::Only;
	}

	/**
	 * @param THttpSessionCookieMode how to use cookie to store session ID
	 * @throws TInvalidOperationException if session is started already
	 */
	public function setCookieMode($value)
	{
		if($this->_started)
			throw new TInvalidOperationException('httpsession_cookiemode_unchangeable');
		else
		{
			$value=TPropertyValue::ensureEnum($value,'THttpSessionCookieMode');
			if($value===THttpSessionCookieMode::None)
				ini_set('session.use_cookies','0');
			else if($value===THttpSessionCookieMode::Allow)
			{
				ini_set('session.use_cookies','1');
				ini_set('session.use_only_cookies','0');
			}
			else
			{
				ini_set('session.use_cookies','1');
				ini_set('session.use_only_cookies','1');
				ini_set('session.use_trans_sid', 0);
			}
		}
	}

	/**
	 * @return boolean whether the session should be automatically started when the session module is initialized, defaults to false.
	 */
	public function getAutoStart()
	{
		return $this->_autoStart;
	}

	/**
	 * @param boolean whether the session should be automatically started when the session module is initialized, defaults to false.
	 * @throws TInvalidOperationException if session is started already
	 */
	public function setAutoStart($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('httpsession_autostart_unchangeable');
		else
			$this->_autoStart=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return integer the probability (percentage) that the gc (garbage collection) process is started on every session initialization, defaults to 1 meaning 1% chance.
	 */
	public function getGCProbability()
	{
		return TPropertyValue::ensureInteger(ini_get('session.gc_probability'));
	}

	/**
	 * @param integer the probability (percentage) that the gc (garbage collection) process is started on every session initialization.
	 * @throws TInvalidOperationException if session is started already
	 * @throws TInvalidDataValueException if the value is beyond [0,100].
	 */
	public function setGCProbability($value)
	{
		if($this->_started)
			throw new TInvalidOperationException('httpsession_gcprobability_unchangeable');
		else
		{
			$value=TPropertyValue::ensureInteger($value);
			if($value>=0 && $value<=100)
			{
				ini_set('session.gc_probability',$value);
				ini_set('session.gc_divisor','100');
			}
			else
				throw new TInvalidDataValueException('httpsession_gcprobability_invalid',$value);
		}
	}

	/**
	 * @return boolean whether transparent sid support is enabled or not, defaults to false.
	 */
	public function getUseTransparentSessionID()
	{
		return ini_get('session.use_trans_sid')==='1';
	}

	/**
	 * @param boolean whether transparent sid support is enabled or not.
	 */
	public function setUseTransparentSessionID($value)
	{
		if($this->_started)
			throw new TInvalidOperationException('httpsession_transid_unchangeable');
		else
		{
			$value=TPropertyValue::ensureBoolean($value);
			if ($value && $this->getCookieMode()==THttpSessionCookieMode::Only)
					throw new TInvalidOperationException('httpsession_transid_cookieonly');
			ini_set('session.use_trans_sid',$value?'1':'0');
		}
	}

	/**
	 * @return integer the number of seconds after which data will be seen as 'garbage' and cleaned up, defaults to 1440 seconds.
	 */
	public function getTimeout()
	{
		return TPropertyValue::ensureInteger(ini_get('session.gc_maxlifetime'));
	}

	/**
	 * @param integer the number of seconds after which data will be seen as 'garbage' and cleaned up
	 * @throws TInvalidOperationException if session is started already
	 */
	public function setTimeout($value)
	{
		if($this->_started)
			throw new TInvalidOperationException('httpsession_maxlifetime_unchangeable');
		else
			ini_set('session.gc_maxlifetime',$value);
	}

	/**
	 * Session open handler.
	 * This method should be overridden if {@link setUseCustomStorage UseCustomStorage} is set true.
	 * @param string session save path
	 * @param string session name
	 * @return boolean whether session is opened successfully
	 */
	public function _open($savePath,$sessionName)
	{
		return true;
	}

	/**
	 * Session close handler.
	 * This method should be overridden if {@link setUseCustomStorage UseCustomStorage} is set true.
	 * @return boolean whether session is closed successfully
	 */
	public function _close()
	{
		return true;
	}

	/**
	 * Session read handler.
	 * This method should be overridden if {@link setUseCustomStorage UseCustomStorage} is set true.
	 * @param string session ID
	 * @return string the session data
	 */
	public function _read($id)
	{
		return '';
	}

	/**
	 * Session write handler.
	 * This method should be overridden if {@link setUseCustomStorage UseCustomStorage} is set true.
	 * @param string session ID
	 * @param string session data
	 * @return boolean whether session write is successful
	 */
	public function _write($id,$data)
	{
		return true;
	}

	/**
	 * Session destroy handler.
	 * This method should be overridden if {@link setUseCustomStorage UseCustomStorage} is set true.
	 * @param string session ID
	 * @return boolean whether session is destroyed successfully
	 */
	public function _destroy($id)
	{
		return true;
	}

	/**
	 * Session GC (garbage collection) handler.
	 * This method should be overridden if {@link setUseCustomStorage UseCustomStorage} is set true.
	 * @param integer the number of seconds after which data will be seen as 'garbage' and cleaned up.
	 * @return boolean whether session is GCed successfully
	 */
	public function _gc($maxLifetime)
	{
		return true;
	}

	//------ The following methods enable THttpSession to be TMap-like -----

	/**
	 * Returns an iterator for traversing the session variables.
	 * This method is required by the interface IteratorAggregate.
	 * @return TSessionIterator an iterator for traversing the session variables.
	 */
	public function getIterator()
	{
		return new TSessionIterator;
	}

	/**
	 * @return integer the number of session variables
	 */
	public function getCount()
	{
		return count($_SESSION);
	}

	/**
	 * Returns the number of items in the session.
	 * This method is required by Countable interface.
	 * @return integer number of items in the session.
	 */
	public function count()
	{
		return $this->getCount();
	}

	/**
	 * @return array the list of session variable names
	 */
	public function getKeys()
	{
		return array_keys($_SESSION);
	}

	/**
	 * Returns the session variable value with the session variable name.
	 * This method is exactly the same as {@link offsetGet}.
	 * @param mixed the session variable name
	 * @return mixed the session variable value, null if no such variable exists
	 */
	public function itemAt($key)
	{
		return isset($_SESSION[$key]) ? $_SESSION[$key] : null;
	}

	/**
	 * Adds a session variable.
	 * Note, if the specified name already exists, the old value will be removed first.
	 * @param mixed session variable name
	 * @param mixed session variable value
	 */
	public function add($key,$value)
	{
		$_SESSION[$key]=$value;
	}

	/**
	 * Removes a session variable.
	 * @param mixed the name of the session variable to be removed
	 * @return mixed the removed value, null if no such session variable.
	 */
	public function remove($key)
	{
		if(isset($_SESSION[$key]))
		{
			$value=$_SESSION[$key];
			unset($_SESSION[$key]);
			return $value;
		}
		else
			return null;
	}

	/**
	 * Removes all session variables
	 */
	public function clear()
	{
		foreach(array_keys($_SESSION) as $key)
			unset($_SESSION[$key]);
	}

	/**
	 * @param mixed session variable name
	 * @return boolean whether there is the named session variable
	 */
	public function contains($key)
	{
		return isset($_SESSION[$key]);
	}

	/**
	 * @return array the list of all session variables in array
	 */
	public function toArray()
	{
		return $_SESSION;
	}

	/**
	 * This method is required by the interface ArrayAccess.
	 * @param mixed the offset to check on
	 * @return boolean
	 */
	public function offsetExists($offset)
	{
		return isset($_SESSION[$offset]);
	}

	/**
	 * This method is required by the interface ArrayAccess.
	 * @param integer the offset to retrieve element.
	 * @return mixed the element at the offset, null if no element is found at the offset
	 */
	public function offsetGet($offset)
	{
		return isset($_SESSION[$offset]) ? $_SESSION[$offset] : null;
	}

	/**
	 * This method is required by the interface ArrayAccess.
	 * @param integer the offset to set element
	 * @param mixed the element value
	 */
	public function offsetSet($offset,$item)
	{
		$_SESSION[$offset]=$item;
	}

	/**
	 * This method is required by the interface ArrayAccess.
	 * @param mixed the offset to unset element
	 */
	public function offsetUnset($offset)
	{
		unset($_SESSION[$offset]);
	}
}

/**
 * TSessionIterator class
 *
 * TSessionIterator implements Iterator interface.
 *
 * TSessionIterator is used by THttpSession. It allows THttpSession to return a new iterator
 * for traversing the session variables.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpSession.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web
 * @since 3.0
 */
class TSessionIterator implements Iterator
{
	/**
	 * @var array list of keys in the map
	 */
	private $_keys;
	/**
	 * @var mixed current key
	 */
	private $_key;

	/**
	 * Constructor.
	 * @param array the data to be iterated through
	 */
	public function __construct()
	{
		$this->_keys=array_keys($_SESSION);
	}

	/**
	 * Rewinds internal array pointer.
	 * This method is required by the interface Iterator.
	 */
	public function rewind()
	{
		$this->_key=reset($this->_keys);
	}

	/**
	 * Returns the key of the current array element.
	 * This method is required by the interface Iterator.
	 * @return mixed the key of the current array element
	 */
	public function key()
	{
		return $this->_key;
	}

	/**
	 * Returns the current array element.
	 * This method is required by the interface Iterator.
	 * @return mixed the current array element
	 */
	public function current()
	{
		return isset($_SESSION[$this->_key])?$_SESSION[$this->_key]:null;
	}

	/**
	 * Moves the internal pointer to the next array element.
	 * This method is required by the interface Iterator.
	 */
	public function next()
	{
		do
		{
			$this->_key=next($this->_keys);
		}
		while(!isset($_SESSION[$this->_key]) && $this->_key!==false);
	}

	/**
	 * Returns whether there is an element at current position.
	 * This method is required by the interface Iterator.
	 * @return boolean
	 */
	public function valid()
	{
		return $this->_key!==false;
	}
}


/**
 * THttpSessionCookieMode class.
 * THttpSessionCookieMode defines the enumerable type for the possible methods of
 * using cookies to store session ID.
 *
 * The following enumerable values are defined:
 * - None: not using cookie.
 * - Allow: using cookie.
 * - Only: using cookie only.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THttpSession.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web
 * @since 3.0.4
 */
class THttpSessionCookieMode extends TEnumerable
{
	const None='None';
	const Allow='Allow';
	const Only='Only';
}

