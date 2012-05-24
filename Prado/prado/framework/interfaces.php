<?php
/**
 * Core interfaces essential for TApplication class.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 */

/**
 * IModule interface.
 *
 * This interface must be implemented by application modules.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface IModule
{
	/**
	 * Initializes the module.
	 * @param TXmlElement the configuration for the module
	 */
	public function init($config);
	/**
	 * @return string ID of the module
	 */
	public function getID();
	/**
	 * @param string ID of the module
	 */
	public function setID($id);
}

/**
 * IService interface.
 *
 * This interface must be implemented by services.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface IService
{
	/**
	 * Initializes the service.
	 * @param TXmlElement the configuration for the service
	 */
	public function init($config);
	/**
	 * @return string ID of the service
	 */
	public function getID();
	/**
	 * @param string ID of the service
	 */
	public function setID($id);
	/**
	 * @return boolean whether the service is enabled
	 */
	public function getEnabled();
	/**
	 * @param boolean whether the service is enabled
	 */
	public function setEnabled($value);
	/**
	 * Runs the service.
	 */
	public function run();
}

/**
 * ITextWriter interface.
 *
 * This interface must be implemented by writers.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface ITextWriter
{
	/**
	 * Writes a string.
	 * @param string string to be written
	 */
	public function write($str);
	/**
	 * Flushes the content that has been written.
	 */
	public function flush();
}


/**
 * IUser interface.
 *
 * This interface must be implemented by user objects.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface IUser
{
	/**
	 * @return string username
	 */
	public function getName();
	/**
	 * @param string username
	 */
	public function setName($value);
	/**
	 * @return boolean if the user is a guest
	 */
	public function getIsGuest();
	/**
	 * @param boolean if the user is a guest
	 */
	public function setIsGuest($value);
	/**
	 * @return array list of roles that the user is of
	 */
	public function getRoles();
	/**
	 * @return array|string list of roles that the user is of. If it is a string, roles are assumed by separated by comma
	 */
	public function setRoles($value);
	/**
	 * @param string role to be tested
	 * @return boolean whether the user is of this role
	 */
	public function isInRole($role);
	/**
	 * @return string user data that is serialized and will be stored in session
	 */
	public function saveToString();
	/**
	 * @param string user data that is serialized and restored from session
	 * @return IUser the user object
	 */
	public function loadFromString($string);
}

/**
 * IStatePersister class.
 *
 * This interface must be implemented by all state persister classes (such as
 * {@link TPageStatePersister}, {@link TApplicationStatePersister}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface IStatePersister
{
	/**
	 * Loads state from a persistent storage.
	 * @return mixed the state
	 */
	public function load();
	/**
	 * Saves state into a persistent storage.
	 * @param mixed the state to be saved
	 */
	public function save($state);
}


/**
 * ICache interface.
 *
 * This interface must be implemented by cache managers.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface ICache
{
	/**
	 * Retrieves a value from cache with a specified key.
	 * @param string a key identifying the cached value
	 * @return mixed the value stored in cache, false if the value is not in the cache or expired.
	 */
	public function get($id);
	/**
	 * Stores a value identified by a key into cache.
	 * If the cache already contains such a key, the existing value and
	 * expiration time will be replaced with the new ones.
	 *
	 * @param string the key identifying the value to be cached
	 * @param mixed the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @param ICacheDependency dependency of the cached item. If the dependency changes, the item is labelled invalid.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	public function set($id,$value,$expire=0,$dependency=null);
	/**
	 * Stores a value identified by a key into cache if the cache does not contain this key.
	 * Nothing will be done if the cache already contains the key.
	 * @param string the key identifying the value to be cached
	 * @param mixed the value to be cached
	 * @param integer the number of seconds in which the cached value will expire. 0 means never expire.
	 * @param ICacheDependency dependency of the cached item. If the dependency changes, the item is labelled invalid.
	 * @return boolean true if the value is successfully stored into cache, false otherwise
	 */
	public function add($id,$value,$expire=0,$dependency=null);
	/**
	 * Deletes a value with the specified key from cache
	 * @param string the key of the value to be deleted
	 * @return boolean if no error happens during deletion
	 */
	public function delete($id);
	/**
	 * Deletes all values from cache.
	 * Be careful of performing this operation if the cache is shared by multiple applications.
	 */
	public function flush();
}

/**
 * ICacheDependency interface.
 *
 * This interface must be implemented by classes meant to be used as
 * cache dependencies.
 *
 * Classes implementing this interface must support serialization and unserialization.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface ICacheDependency
{
	/**
	 * @return boolean whether the dependency has changed. Defaults to false.
	 */
	public function getHasChanged();
}

/**
 * IRenderable interface.
 *
 * This interface must be implemented by classes that can be rendered
 * to end-users.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface IRenderable
{
	/**
	 * Renders the component to end-users.
	 * @param ITextWriter writer for the rendering purpose
	 */
	public function render($writer);
}

/**
 * IBindable interface.
 *
 * This interface must be implemented by classes that are capable of performing databinding.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
interface IBindable
{
	/**
	 * Performs databinding.
	 */
	public function dataBind();
}

/**
 * IStyleable interface.
 *
 * This interface should be implemented by classes that support CSS styles.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.1.0
 */
interface IStyleable
{
	/**
	 * @return boolean whether the object has defined any style information
	 */
	public function getHasStyle();
	/**
	 * @return TStyle the object representing the css style of the object
	 */
	public function getStyle();
	/**
	 * Removes all styles associated with the object
	 */
	public function clearStyle();
}

/**
 * IActiveControl interface.
 *
 * Active controls must implement IActiveControl interface.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.1
 */
interface IActiveControl
{
	/**
	 * @return TBaseActiveControl Active control properties.
	 */
	public function getActiveControl();
}

/**
 * ICallbackEventHandler interface.
 *
 * If a control wants to respond to callback event, it must implement this
 * interface.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.1
 */
interface ICallbackEventHandler
{
	/**
	 * Raises callback event. The implementation of this function should raise
	 * appropriate event(s) (e.g. OnClick, OnCommand) indicating the component
	 * is responsible for the callback event.
	 * @param TCallbackEventParameter the parameter associated with the callback event
	 */
	public function raiseCallbackEvent($eventArgument);
}

/**
 * IDataRenderer interface.
 *
 * If a control wants to be used a renderer for another data-bound control,
 * this interface must be implemented.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: interfaces.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.1
 */
interface IDataRenderer
{
	/**
	 * @return mixed the data bound to this object
	 */
	public function getData();

	/**
	 * @param mixed the data to be bound to this object
	 */
	public function setData($value);
}
