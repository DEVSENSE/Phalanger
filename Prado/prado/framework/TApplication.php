<?php
/**
 * TApplication class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TApplication.php 2704 2009-09-15 08:23:47Z Christophe.Boulain $
 * @package System
 */

/**
 * Includes core interfaces essential for TApplication class
 */
require_once(PRADO_DIR.'/interfaces.php');

/**
 * Includes core classes essential for TApplication class
 */
Prado::using('System.TApplicationComponent');
Prado::using('System.TModule');
Prado::using('System.TService');
Prado::using('System.Exceptions.TErrorHandler');
Prado::using('System.Caching.TCache');
Prado::using('System.IO.TTextWriter');
Prado::using('System.Collections.TList');
Prado::using('System.Collections.TMap');
Prado::using('System.Collections.TStack');
Prado::using('System.Xml.TXmlDocument');
Prado::using('System.Security.TAuthorizationRule');
Prado::using('System.Security.TSecurityManager');
Prado::using('System.Web.THttpUtility');
Prado::using('System.Web.Javascripts.TJavaScript');
Prado::using('System.Web.THttpRequest');
Prado::using('System.Web.THttpResponse');
Prado::using('System.Web.THttpSession');
Prado::using('System.Web.Services.TPageService');
Prado::using('System.Web.TAssetManager');
Prado::using('System.I18N.TGlobalization');

/**
 * TApplication class.
 *
 * TApplication coordinates modules and services, and serves as a configuration
 * context for all Prado components.
 *
 * TApplication uses a configuration file to specify the settings of
 * the application, the modules, the services, the parameters, and so on.
 *
 * TApplication adopts a modular structure. A TApplication instance is a composition
 * of multiple modules. A module is an instance of class implementing
 * {@link IModule} interface. Each module accomplishes certain functionalities
 * that are shared by all Prado components in an application.
 * There are default modules and user-defined modules. The latter offers extreme
 * flexibility of extending TApplication in a plug-and-play fashion.
 * Modules cooperate with each other to serve a user request by following
 * a sequence of lifecycles predefined in TApplication.
 *
 * TApplication has four modes that can be changed by setting {@link setMode Mode}
 * property (in the application configuration file).
 * - <b>Off</b> mode will prevent the application from serving user requests.
 * - <b>Debug</b> mode is mainly used during application development. It ensures
 *   the cache is always up-to-date if caching is enabled. It also allows
 *   exceptions are displayed with rich context information if they occur.
 * - <b>Normal</b> mode is mainly used during production stage. Exception information
 *   will only be recorded in system error logs. The cache is ensured to be
 *   up-to-date if it is enabled.
 * - <b>Performance</b> mode is similar to <b>Normal</b> mode except that it
 *   does not ensure the cache is up-to-date.
 *
 * TApplication dispatches each user request to a particular service which
 * finishes the actual work for the request with the aid from the application
 * modules.
 *
 * TApplication maintains a lifecycle with the following stages:
 * - [construct] : construction of the application instance
 * - [initApplication] : load application configuration and instantiate modules and the requested service
 * - onBeginRequest : this event happens right after application initialization
 * - onAuthentication : this event happens when authentication is needed for the current request
 * - onAuthenticationComplete : this event happens right after the authentication is done for the current request
 * - onAuthorization : this event happens when authorization is needed for the current request
 * - onAuthorizationComplete : this event happens right after the authorization is done for the current request
 * - onLoadState : this event happens when application state needs to be loaded
 * - onLoadStateComplete : this event happens right after the application state is loaded
 * - onPreRunService : this event happens right before the requested service is to run
 * - runService : the requested service runs
 * - onSaveState : this event happens when application needs to save its state
 * - onSaveStateComplete : this event happens right after the application saves its state
 * - onPreFlushOutput : this event happens right before the application flushes output to client side.
 * - flushOutput : the application flushes output to client side.
 * - onEndRequest : this is the last stage a request is being completed
 * - [destruct] : destruction of the application instance
 * Modules and services can attach their methods to one or several of the above
 * events and do appropriate processing when the events are raised. By this way,
 * the application is able to coordinate the activities of modules and services
 * in the above order. To terminate an application before the whole lifecycle
 * completes, call {@link completeRequest}.
 *
 * Examples:
 * - Create and run a Prado application:
 * <code>
 * $application=new TApplication($configFile);
 * $application->run();
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TApplication.php 2704 2009-09-15 08:23:47Z Christophe.Boulain $
 * @package System
 * @since 3.0
 */
class TApplication extends TComponent
{
	/**
	 * possible application mode.
	 * @deprecated deprecated since version 3.0.4 (use TApplicationMode constants instead)
	 */
	const STATE_OFF='Off';
	const STATE_DEBUG='Debug';
	const STATE_NORMAL='Normal';
	const STATE_PERFORMANCE='Performance';

	/**
	 * Page service ID
	 */
	const PAGE_SERVICE_ID='page';
	/**
	 * Application configuration file name
	 */
	const CONFIG_FILE='application.xml';
	/**
	 * File extension for external config files
	 */
	const CONFIG_FILE_EXT='.xml';
	/**
	 * Runtime directory name
	 */
	const RUNTIME_PATH='runtime';
	/**
	 * Config cache file
	 */
	const CONFIGCACHE_FILE='config.cache';
	/**
	 * Global data file
	 */
	const GLOBAL_FILE='global.cache';

	/**
	 * @var array list of events that define application lifecycles
	 */
	private static $_steps=array(
		'onBeginRequest',
		'onLoadState',
		'onLoadStateComplete',
		'onAuthentication',
		'onAuthenticationComplete',
		'onAuthorization',
		'onAuthorizationComplete',
		'onPreRunService',
		'runService',
		'onSaveState',
		'onSaveStateComplete',
		'onPreFlushOutput',
		'flushOutput'
	);

	/**
	 * @var string application ID
	 */
	private $_id;
	/**
	 * @var string unique application ID
	 */
	private $_uniqueID;
	/**
	 * @var boolean whether the request is completed
	 */
	private $_requestCompleted=false;
	/**
	 * @var integer application state
	 */
	private $_step;
	/**
	 * @var array available services and their configurations indexed by service IDs
	 */
	private $_services;
	/**
	 * @var IService current service instance
	 */
	private $_service;
	/**
	 * @var array list of application modules
	 */
	private $_modules=array();
	/**
	 * @var TMap list of application parameters
	 */
	private $_parameters;
	/**
	 * @var string configuration file
	 */
	private $_configFile;
	/**
	 * @var string application base path
	 */
	private $_basePath;
	/**
	 * @var string directory storing application state
	 */
	private $_runtimePath;
	/**
	 * @var boolean if any global state is changed during the current request
	 */
	private $_stateChanged=false;
	/**
	 * @var array global variables (persistent across sessions, requests)
	 */
	private $_globals=array();
	/**
	 * @var string cache file
	 */
	private $_cacheFile;
	/**
	 * @var TErrorHandler error handler module
	 */
	private $_errorHandler;
	/**
	 * @var THttpRequest request module
	 */
	private $_request;
	/**
	 * @var THttpResponse response module
	 */
	private $_response;
	/**
	 * @var THttpSession session module, could be null
	 */
	private $_session;
	/**
	 * @var ICache cache module, could be null
	 */
	private $_cache;
	/**
	 * @var IStatePersister application state persister
	 */
	private $_statePersister;
	/**
	 * @var IUser user instance, could be null
	 */
	private $_user;
	/**
	 * @var TGlobalization module, could be null
	 */
	private $_globalization;
	/**
	 * @var TSecurityManager security manager module
	 */
	private $_security;
	/**
	 * @var TAssetManager asset manager module
	 */
	private $_assetManager;
	/**
	 * @var TAuthorizationRuleCollection collection of authorization rules
	 */
	private $_authRules;
	/**
	 * @var TApplicationMode application mode
	 */
	private $_mode=TApplicationMode::Debug;
	
	/**
	 * @var string Customizable page service ID
	 */
	private $_pageServiceID = self::PAGE_SERVICE_ID;

	/**
	 * Constructor.
	 * Sets application base path and initializes the application singleton.
	 * Application base path refers to the root directory storing application
	 * data and code not directly accessible by Web users.
	 * By default, the base path is assumed to be the <b>protected</b>
	 * directory under the directory containing the current running script.
	 * @param string application base path or configuration file path.
	 *        If the parameter is a file, it is assumed to be the application
	 *        configuration file, and the directory containing the file is treated
	 *        as the application base path.
	 *        If it is a directory, it is assumed to be the application base path,
	 *        and within that directory, a file named <b>application.xml</b>
	 *        will be looked for. If found, the file is considered as the application
	 *        configuration file.
	 * @param boolean whether to cache application configuration. Defaults to true.
	 * @throws TConfigurationException if configuration file cannot be read or the runtime path is invalid.
	 */
	public function __construct($basePath='protected',$cacheConfig=true)
	{
		// register application as a singleton
		Prado::setApplication($this);

		$this->resolvePaths($basePath);

		if($cacheConfig)
			$this->_cacheFile=$this->_runtimePath.DIRECTORY_SEPARATOR.self::CONFIGCACHE_FILE;

		// generates unique ID by hashing the runtime path
		$this->_uniqueID=md5($this->_runtimePath);
		$this->_parameters=new TMap;
		$this->_services=array($this->getPageServiceID()=>array('TPageService',array(),null));
		
		Prado::setPathOfAlias('Application',$this->_basePath);
	}

	/**
	 * Resolves application-relevant paths.
	 * This method is invoked by the application constructor
	 * to determine the application configuration file,
	 * application root path and the runtime path.
	 * @param string the application root path or the application configuration file
	 * @see setBasePath
	 * @see setRuntimePath
	 * @see setConfigurationFile
	 */
	protected function resolvePaths($basePath)
	{
		// determine configuration path and file
		if(empty($basePath) || ($basePath=realpath($basePath))===false)
			throw new TConfigurationException('application_basepath_invalid',$basePath);
		if(is_file($basePath.DIRECTORY_SEPARATOR.self::CONFIG_FILE))
			$configFile=$basePath.DIRECTORY_SEPARATOR.self::CONFIG_FILE;
		else if(is_file($basePath))
		{
			$configFile=$basePath;
			$basePath=dirname($configFile);
		}
		else
			$configFile=null;

		// determine runtime path
		$runtimePath=$basePath.DIRECTORY_SEPARATOR.self::RUNTIME_PATH;
		if(is_writable($runtimePath))
		{
			if($configFile!==null)
			{
				$runtimePath.=DIRECTORY_SEPARATOR.basename($configFile).'-'.Prado::getVersion();
				if(!is_dir($runtimePath))
				{
					if(@mkdir($runtimePath)===false)
						throw new TConfigurationException('application_runtimepath_failed',$runtimePath);
					@chmod($runtimePath, PRADO_CHMOD); //make it deletable
				}
				$this->setConfigurationFile($configFile);
			}
			$this->setBasePath($basePath);
			$this->setRuntimePath($runtimePath);
		}
		else
			throw new TConfigurationException('application_runtimepath_invalid',$runtimePath);

	}

	/**
	 * Executes the lifecycles of the application.
	 * This is the main entry function that leads to the running of the whole
	 * Prado application.
	 */
	public function run()
	{
		try
		{
			$this->initApplication();
			$n=count(self::$_steps);
			$this->_step=0;
			$this->_requestCompleted=false;
			while($this->_step<$n)
			{
				if($this->_mode===self::STATE_OFF)
					throw new THttpException(503,'application_unavailable');
				if($this->_requestCompleted)
					break;
				$method=self::$_steps[$this->_step];
				Prado::trace("Executing $method()",'System.TApplication');
				$this->$method();
				$this->_step++;
			}
		}
		catch(Exception $e)
		{
			$this->onError($e);
		}
		$this->onEndRequest();
	}

	/**
	 * Completes current request processing.
	 * This method can be used to exit the application lifecycles after finishing
	 * the current cycle.
	 */
	public function completeRequest()
	{
		$this->_requestCompleted=true;
	}

	/**
	 * @return boolean whether the current request is processed.
	 */
	public function getRequestCompleted()
	{
		return $this->_requestCompleted;
	}

	/**
	 * Returns a global value.
	 *
	 * A global value is one that is persistent across users sessions and requests.
	 * @param string the name of the value to be returned
	 * @param mixed the default value. If $key is not found, $defaultValue will be returned
	 * @return mixed the global value corresponding to $key
	 */
	public function getGlobalState($key,$defaultValue=null)
	{
		return isset($this->_globals[$key])?$this->_globals[$key]:$defaultValue;
	}

	/**
	 * Sets a global value.
	 *
	 * A global value is one that is persistent across users sessions and requests.
	 * Make sure that the value is serializable and unserializable.
	 * @param string the name of the value to be set
	 * @param mixed the global value to be set
	 * @param mixed the default value. If $key is not found, $defaultValue will be returned
	 */
	public function setGlobalState($key,$value,$defaultValue=null)
	{
		$this->_stateChanged=true;
		if($value===$defaultValue)
			unset($this->_globals[$key]);
		else
			$this->_globals[$key]=$value;
	}

	/**
	 * Clears a global value.
	 *
	 * The value cleared will no longer be available in this request and the following requests.
	 * @param string the name of the value to be cleared
	 */
	public function clearGlobalState($key)
	{
		$this->_stateChanged=true;
		unset($this->_globals[$key]);
	}

	/**
	 * Loads global values from persistent storage.
	 * This method is invoked when {@link onLoadState OnLoadState} event is raised.
	 * After this method, values that are stored in previous requests become
	 * available to the current request via {@link getGlobalState}.
	 */
	protected function loadGlobals()
	{
		$this->_globals=$this->getApplicationStatePersister()->load();
	}

	/**
	 * Saves global values into persistent storage.
	 * This method is invoked when {@link onSaveState OnSaveState} event is raised.
	 */
	protected function saveGlobals()
	{
		if($this->_stateChanged)
		{
			$this->_stateChanged=false;
			$this->getApplicationStatePersister()->save($this->_globals);
		}
	}

	/**
	 * @return string application ID
	 */
	public function getID()
	{
		return $this->_id;
	}

	/**
	 * @param string application ID
	 */
	public function setID($value)
	{
		$this->_id=$value;
	}
	
	/**
	 * @return string page service ID
	 */
	public function getPageServiceID()
	{
		return $this->_pageServiceID;
	}

	/**
	 * @param string page service ID
	 */
	public function setPageServiceID($value)
	{
		$this->_pageServiceID=$value;
	}

	/**
	 * @return string an ID that uniquely identifies this Prado application from the others
	 */
	public function getUniqueID()
	{
		return $this->_uniqueID;
	}

	/**
	 * @return TApplicationMode application mode. Defaults to TApplicationMode::Debug.
	 */
	public function getMode()
	{
		return $this->_mode;
	}

	/**
	 * @param TApplicationMode application mode
	 */
	public function setMode($value)
	{
		$this->_mode=TPropertyValue::ensureEnum($value,'TApplicationMode');
	}

	/**
	 * @return string the directory containing the application configuration file (absolute path)
	 */
	public function getBasePath()
	{
		return $this->_basePath;
	}

	/**
	 * @param string the directory containing the application configuration file
	 */
	public function setBasePath($value)
	{
		$this->_basePath=$value;
	}

	/**
	 * @return string the application configuration file (absolute path)
	 */
	public function getConfigurationFile()
	{
		return $this->_configFile;
	}

	/**
	 * @param string the application configuration file (absolute path)
	 */
	public function setConfigurationFile($value)
	{
		$this->_configFile=$value;
	}

	/**
	 * @return string the directory storing cache data and application-level persistent data. (absolute path)
	 */
	public function getRuntimePath()
	{
		return $this->_runtimePath;
	}

	/**
	 * @param string the directory storing cache data and application-level persistent data. (absolute path)
	 */
	public function setRuntimePath($value)
	{
		$this->_runtimePath=$value;
		if($this->_cacheFile)
			$this->_cacheFile=$this->_runtimePath.DIRECTORY_SEPARATOR.self::CONFIGCACHE_FILE;
		// generates unique ID by hashing the runtime path
		$this->_uniqueID=md5($this->_runtimePath);
	}

	/**
	 * @return IService the currently requested service
	 */
	public function getService()
	{
		return $this->_service;
	}

	/**
	 * @param IService the currently requested service
	 */
	public function setService($value)
	{
		$this->_service=$value;
	}

	/**
	 * Adds a module to application.
	 * Note, this method does not do module initialization.
	 * @param string ID of the module
	 * @param IModule module object
	 */
	public function setModule($id,IModule $module)
	{
		if(isset($this->_modules[$id]))
			throw new TConfigurationException('application_moduleid_duplicated',$id);
		else
			$this->_modules[$id]=$module;
	}

	/**
	 * @return IModule the module with the specified ID, null if not found
	 */
	public function getModule($id)
	{
		return isset($this->_modules[$id])?$this->_modules[$id]:null;
	}

	/**
	 * @return array list of loaded application modules, indexed by module IDs
	 */
	public function getModules()
	{
		return $this->_modules;
	}

	/**
	 * Returns the list of application parameters.
	 * Since the parameters are returned as a {@link TMap} object, you may use
	 * the returned result to access, add or remove individual parameters.
	 * @return TMap the list of application parameters
	 */
	public function getParameters()
	{
		return $this->_parameters;
	}

	/**
	 * @return THttpRequest the request module
	 */
	public function getRequest()
	{
		if(!$this->_request)
		{
			$this->_request=new THttpRequest;
			$this->_request->init(null);
		}
		return $this->_request;
	}

	/**
	 * @param THttpRequest the request module
	 */
	public function setRequest(THttpRequest $request)
	{
		$this->_request=$request;
	}

	/**
	 * @return THttpResponse the response module
	 */
	public function getResponse()
	{
		if(!$this->_response)
		{
			$this->_response=new THttpResponse;
			$this->_response->init(null);
		}
		return $this->_response;
	}

	/**
	 * @param THttpRequest the request module
	 */
	public function setResponse(THttpResponse $response)
	{
		$this->_response=$response;
	}

	/**
	 * @return THttpSession the session module, null if session module is not installed
	 */
	public function getSession()
	{
		if(!$this->_session)
		{
			$this->_session=new THttpSession;
			$this->_session->init(null);
		}
		return $this->_session;
	}

	/**
	 * @param THttpSession the session module
	 */
	public function setSession(THttpSession $session)
	{
		$this->_session=$session;
	}

	/**
	 * @return TErrorHandler the error handler module
	 */
	public function getErrorHandler()
	{
		if(!$this->_errorHandler)
		{
			$this->_errorHandler=new TErrorHandler;
			$this->_errorHandler->init(null);
		}
		return $this->_errorHandler;
	}

	/**
	 * @param TErrorHandler the error handler module
	 */
	public function setErrorHandler(TErrorHandler $handler)
	{
		$this->_errorHandler=$handler;
	}

	/**
	 * @return TSecurityManager the security manager module
	 */
	public function getSecurityManager()
	{
		if(!$this->_security)
		{
			$this->_security=new TSecurityManager;
			$this->_security->init(null);
		}
		return $this->_security;
	}

	/**
	 * @param TSecurityManager the security manager module
	 */
	public function setSecurityManager(TSecurityManager $sm)
	{
		$this->_security=$sm;
	}

	/**
	 * @return TAssetManager asset manager
	 */
	public function getAssetManager()
	{
		if(!$this->_assetManager)
		{
			$this->_assetManager=new TAssetManager;
			$this->_assetManager->init(null);
		}
		return $this->_assetManager;
	}

	/**
	 * @param TAssetManager asset manager
	 */
	public function setAssetManager(TAssetManager $value)
	{
		$this->_assetManager=$value;
	}

	/**
	 * @return IStatePersister application state persister
	 */
	public function getApplicationStatePersister()
	{
		if(!$this->_statePersister)
		{
			$this->_statePersister=new TApplicationStatePersister;
			$this->_statePersister->init(null);
		}
		return $this->_statePersister;
	}

	/**
	 * @param IStatePersister  application state persister
	 */
	public function setApplicationStatePersister(IStatePersister $persister)
	{
		$this->_statePersister=$persister;
	}

	/**
	 * @return ICache the cache module, null if cache module is not installed
	 */
	public function getCache()
	{
		return $this->_cache;
	}

	/**
	 * @param ICache the cache module
	 */
	public function setCache(ICache $cache)
	{
		$this->_cache=$cache;
	}

	/**
	 * @return IUser the application user
	 */
	public function getUser()
	{
		return $this->_user;
	}

	/**
	 * @param IUser the application user
	 */
	public function setUser(IUser $user)
	{
		$this->_user=$user;
	}

	/**
	 * @param boolean whether to create globalization if it does not exist
	 * @return TGlobalization globalization module
	 */
	public function getGlobalization($createIfNotExists=true)
	{
		if($this->_globalization===null && $createIfNotExists)
			$this->_globalization=new TGlobalization;
		return $this->_globalization;
	}

	/**
	 * @param TGlobalization globalization module
	 */
	public function setGlobalization(TGlobalization $glob)
	{
		$this->_globalization=$glob;
	}

	/**
	 * @return TAuthorizationRuleCollection list of authorization rules for the current request
	 */
	public function getAuthorizationRules()
	{
		if($this->_authRules===null)
			$this->_authRules=new TAuthorizationRuleCollection;
		return $this->_authRules;
	}

	/**
	 * Applies an application configuration.
	 * @param TApplicationConfiguration the configuration
	 * @param boolean whether the configuration is specified within a service.
	 */
	public function applyConfiguration($config,$withinService=false)
	{
		if($config->getIsEmpty())
			return;

		// set path aliases and using namespaces
		foreach($config->getAliases() as $alias=>$path)
			Prado::setPathOfAlias($alias,$path);
		foreach($config->getUsings() as $using)
			Prado::using($using);

		// set application properties
		if(!$withinService)
		{
			foreach($config->getProperties() as $name=>$value)
				$this->setSubProperty($name,$value);
		}
		
		if(empty($this->_services))
			$this->_services=array($this->getPageServiceID()=>array('TPageService',array(),null));

		// load parameters
		foreach($config->getParameters() as $id=>$parameter)
		{
			if(is_array($parameter))
			{
				$component=Prado::createComponent($parameter[0]);
				foreach($parameter[1] as $name=>$value)
					$component->setSubProperty($name,$value);
				$this->_parameters->add($id,$component);
			}
			else
				$this->_parameters->add($id,$parameter);
		}

		// load and init modules specified in app config
		$modules=array();
		foreach($config->getModules() as $id=>$moduleConfig)
		{
			Prado::trace("Loading module $id ({$moduleConfig[0]})",'System.TApplication');
			list($moduleClass, $initProperties, $configElement)=$moduleConfig;
			$module=Prado::createComponent($moduleClass);
			if(!is_string($id))
			{
				$id='_module'.count($this->_modules);
				$initProperties['id']=$id;
			}
			$this->setModule($id,$module);
			foreach($initProperties as $name=>$value)
				$module->setSubProperty($name,$value);
			$modules[]=array($module,$configElement);
		}
		foreach($modules as $module)
			$module[0]->init($module[1]);

		// load service
		foreach($config->getServices() as $serviceID=>$serviceConfig)
			$this->_services[$serviceID]=$serviceConfig;

		// external configurations
		foreach($config->getExternalConfigurations() as $filePath=>$condition)
		{
			if($condition!==true)
				$condition=$this->evaluateExpression($condition);
			if($condition)
			{
				if(($path=Prado::getPathOfNamespace($filePath,self::CONFIG_FILE_EXT))===null || !is_file($path))
					throw new TConfigurationException('application_includefile_invalid',$filePath);
				$c=new TApplicationConfiguration;
				$c->loadFromFile($path);
				$this->applyConfiguration($c,$withinService);
			}
		}
	}

	/**
	 * Loads configuration and initializes application.
	 * Configuration file will be read and parsed (if a valid cached version exists,
	 * it will be used instead). Then, modules are created and initialized;
	 * Afterwards, the requested service is created and initialized.
	 * @param string configuration file path (absolute or relative to current executing script)
	 * @param string cache file path, empty if no present or needed
	 * @throws TConfigurationException if module is redefined of invalid type, or service not defined or of invalid type
	 */
	protected function initApplication()
	{
		Prado::trace('Initializing application','System.TApplication');

		if($this->_configFile!==null)
		{
			if($this->_cacheFile===null || @filemtime($this->_cacheFile)<filemtime($this->_configFile))
			{
				$config=new TApplicationConfiguration;
				$config->loadFromFile($this->_configFile);
				if($this->_cacheFile!==null)
					file_put_contents($this->_cacheFile,Prado::serialize($config),LOCK_EX);
			}
			else
				$config=Prado::unserialize(file_get_contents($this->_cacheFile));

			$this->applyConfiguration($config,false);
		}

		if(($serviceID=$this->getRequest()->resolveRequest(array_keys($this->_services)))===null)
			$serviceID=$this->getPageServiceID();
		
		$this->startService($serviceID);
	}

	/**
	 * Starts the specified service.
	 * The service instance will be created. Its properties will be initialized
	 * and the configurations will be applied, if any.
	 * @param string service ID
	 */
	public function startService($serviceID)
	{
		if(isset($this->_services[$serviceID]))
		{
			list($serviceClass,$initProperties,$configElement)=$this->_services[$serviceID];
			$service=Prado::createComponent($serviceClass);
			if(!($service instanceof IService))
				throw new THttpException(500,'application_service_invalid',$serviceClass);
			if(!$service->getEnabled())
				throw new THttpException(500,'application_service_unavailable',$serviceClass);
			$service->setID($serviceID);
			$this->setService($service);

			foreach($initProperties as $name=>$value)
				$service->setSubProperty($name,$value);

			if($configElement!==null)
			{
				$config=new TApplicationConfiguration;
				$config->loadFromXml($configElement,$this->getBasePath());
				$this->applyConfiguration($config,true);
			}

			$service->init($configElement);
		}
		else
			throw new THttpException(500,'application_service_unknown',$serviceID);
	}

	/**
	 * Raises OnError event.
	 * This method is invoked when an exception is raised during the lifecycles
	 * of the application.
	 * @param mixed event parameter
	 */
	public function onError($param)
	{
		Prado::log($param->getMessage(),TLogger::ERROR,'System.TApplication');
		$this->raiseEvent('OnError',$this,$param);
		$this->getErrorHandler()->handleError($this,$param);
	}

	/**
	 * Raises OnBeginRequest event.
	 * At the time when this method is invoked, application modules are loaded
	 * and initialized, user request is resolved and the corresponding service
	 * is loaded and initialized. The application is about to start processing
	 * the user request.
	 */
	public function onBeginRequest()
	{
		$this->raiseEvent('OnBeginRequest',$this,null);
	}

	/**
	 * Raises OnAuthentication event.
	 * This method is invoked when the user request needs to be authenticated.
	 */
	public function onAuthentication()
	{
		$this->raiseEvent('OnAuthentication',$this,null);
	}

	/**
	 * Raises OnAuthenticationComplete event.
	 * This method is invoked right after the user request is authenticated.
	 */
	public function onAuthenticationComplete()
	{
		$this->raiseEvent('OnAuthenticationComplete',$this,null);
	}

	/**
	 * Raises OnAuthorization event.
	 * This method is invoked when the user request needs to be authorized.
	 */
	public function onAuthorization()
	{
		$this->raiseEvent('OnAuthorization',$this,null);
	}

	/**
	 * Raises OnAuthorizationComplete event.
	 * This method is invoked right after the user request is authorized.
	 */
	public function onAuthorizationComplete()
	{
		$this->raiseEvent('OnAuthorizationComplete',$this,null);
	}

	/**
	 * Raises OnLoadState event.
	 * This method is invoked when the application needs to load state (probably stored in session).
	 */
	public function onLoadState()
	{
		$this->loadGlobals();
		$this->raiseEvent('OnLoadState',$this,null);
	}

	/**
	 * Raises OnLoadStateComplete event.
	 * This method is invoked right after the application state has been loaded.
	 */
	public function onLoadStateComplete()
	{
		$this->raiseEvent('OnLoadStateComplete',$this,null);
	}

	/**
	 * Raises OnPreRunService event.
	 * This method is invoked right before the service is to be run.
	 */
	public function onPreRunService()
	{
		$this->raiseEvent('OnPreRunService',$this,null);
	}

	/**
	 * Runs the requested service.
	 */
	public function runService()
	{
		if($this->_service)
			$this->_service->run();
	}

	/**
	 * Raises OnSaveState event.
	 * This method is invoked when the application needs to save state (probably stored in session).
	 */
	public function onSaveState()
	{
		$this->raiseEvent('OnSaveState',$this,null);
		$this->saveGlobals();
	}

	/**
	 * Raises OnSaveStateComplete event.
	 * This method is invoked right after the application state has been saved.
	 */
	public function onSaveStateComplete()
	{
		$this->raiseEvent('OnSaveStateComplete',$this,null);
	}

	/**
	 * Raises OnPreFlushOutput event.
	 * This method is invoked right before the application flushes output to client.
	 */
	public function onPreFlushOutput()
	{
		$this->raiseEvent('OnPreFlushOutput',$this,null);
	}

	/**
	 * Flushes output to client side.
	 */
	public function flushOutput()
	{
		$this->getResponse()->flush();
	}

	/**
	 * Raises OnEndRequest event.
	 * This method is invoked when the application completes the processing of the request.
	 */
	public function onEndRequest()
	{
		$this->saveGlobals();  // save global state
		$this->raiseEvent('OnEndRequest',$this,null);
	}
}

/**
 * TApplicationMode class.
 * TApplicationMode defines the possible mode that an application can be set at by
 * setting {@link TApplication::setMode Mode}.
 * In particular, the following modes are defined
 * - Off: the application is not running. Any request to the application will obtain an error.
 * - Debug: the application is running in debug mode.
 * - Normal: the application is running in normal production mode.
 * - Performance: the application is running in performance mode.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TApplication.php 2704 2009-09-15 08:23:47Z Christophe.Boulain $
 * @package System
 * @since 3.0.4
 */
class TApplicationMode extends TEnumerable
{
	const Off='Off';
	const Debug='Debug';
	const Normal='Normal';
	const Performance='Performance';
}


/**
 * TApplicationConfiguration class.
 *
 * This class is used internally by TApplication to parse and represent application configuration.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TApplication.php 2704 2009-09-15 08:23:47Z Christophe.Boulain $
 * @package System
 * @since 3.0
 */
class TApplicationConfiguration extends TComponent
{
	/**
	 * @var array list of application initial property values, indexed by property names
	 */
	private $_properties=array();
	/**
	 * @var array list of namespaces to be used
	 */
	private $_usings=array();
	/**
	 * @var array list of path aliases, indexed by alias names
	 */
	private $_aliases=array();
	/**
	 * @var array list of module configurations
	 */
	private $_modules=array();
	/**
	 * @var array list of service configurations
	 */
	private $_services=array();
	/**
	 * @var array list of parameters
	 */
	private $_parameters=array();
	/**
	 * @var array list of included configurations
	 */
	private $_includes=array();
	/**
	 * @var boolean whether this configuration contains actual stuff
	 */
	private $_empty=true;

	/**
	 * Parses the application configuration file.
	 * @param string configuration file name
	 * @throws TConfigurationException if there is any parsing error
	 */
	public function loadFromFile($fname)
	{
		$dom=new TXmlDocument;
		$dom->loadFromFile($fname);
		$this->loadFromXml($dom,dirname($fname));
	}

	/**
	 * @return boolean whether this configuration contains actual stuff
	 */
	public function getIsEmpty()
	{
		return $this->_empty;
	}

	/**
	 * Parses the application configuration given in terms of a TXmlElement.
	 * @param TXmlElement the XML element
	 * @param string the context path (for specifying relative paths)
	 */
	public function loadFromXml($dom,$configPath)
	{
		// application properties
		foreach($dom->getAttributes() as $name=>$value)
		{
			$this->_properties[$name]=$value;
			$this->_empty=false;
		}

		foreach($dom->getElements() as $element)
		{
			switch($element->getTagName())
			{
				case 'paths':
					$this->loadPathsXml($element,$configPath);
					break;
				case 'modules':
					$this->loadModulesXml($element,$configPath);
					break;
				case 'services':
					$this->loadServicesXml($element,$configPath);
					break;
				case 'parameters':
					$this->loadParametersXml($element,$configPath);
					break;
				case 'include':
					$this->loadExternalXml($element,$configPath);
					break;
				default:
					//throw new TConfigurationException('appconfig_tag_invalid',$element->getTagName());
					break;
			}
		}
	}

	/**
	 * Loads the paths XML node.
	 * @param TXmlElement the paths XML node
	 * @param string the context path (for specifying relative paths)
	 */
	protected function loadPathsXml($pathsNode,$configPath)
	{
		foreach($pathsNode->getElements() as $element)
		{
			switch($element->getTagName())
			{
				case 'alias':
				{
					if(($id=$element->getAttribute('id'))!==null && ($path=$element->getAttribute('path'))!==null)
					{
						$path=str_replace('\\','/',$path);
						if(preg_match('/^\\/|.:\\/|.:\\\\/',$path))	// if absolute path
							$p=realpath($path);
						else
							$p=realpath($configPath.DIRECTORY_SEPARATOR.$path);
						if($p===false || !is_dir($p))
							throw new TConfigurationException('appconfig_aliaspath_invalid',$id,$path);
						if(isset($this->_aliases[$id]))
							throw new TConfigurationException('appconfig_alias_redefined',$id);
						$this->_aliases[$id]=$p;
					}
					else
						throw new TConfigurationException('appconfig_alias_invalid');
					$this->_empty=false;
					break;
				}
				case 'using':
				{
					if(($namespace=$element->getAttribute('namespace'))!==null)
						$this->_usings[]=$namespace;
					else
						throw new TConfigurationException('appconfig_using_invalid');
					$this->_empty=false;
					break;
				}
				default:
					throw new TConfigurationException('appconfig_paths_invalid',$element->getTagName());
			}
		}
	}

	/**
	 * Loads the modules XML node.
	 * @param TXmlElement the modules XML node
	 * @param string the context path (for specifying relative paths)
	 */
	protected function loadModulesXml($modulesNode,$configPath)
	{
		foreach($modulesNode->getElements() as $element)
		{
			if($element->getTagName()==='module')
			{
				$properties=$element->getAttributes();
				$id=$properties->itemAt('id');
				$type=$properties->remove('class');
				if($type===null)
					throw new TConfigurationException('appconfig_moduletype_required',$id);
				$element->setParent(null);
				if($id===null)
					$this->_modules[]=array($type,$properties->toArray(),$element);
				else
					$this->_modules[$id]=array($type,$properties->toArray(),$element);
				$this->_empty=false;
			}
			else
				throw new TConfigurationException('appconfig_modules_invalid',$element->getTagName());
		}
	}

	/**
	 * Loads the services XML node.
	 * @param TXmlElement the services XML node
	 * @param string the context path (for specifying relative paths)
	 */
	protected function loadServicesXml($servicesNode,$configPath)
	{
		foreach($servicesNode->getElements() as $element)
		{
			if($element->getTagName()==='service')
			{
				$properties=$element->getAttributes();
				if(($id=$properties->itemAt('id'))===null)
					throw new TConfigurationException('appconfig_serviceid_required');
				if(($type=$properties->remove('class'))===null)
					throw new TConfigurationException('appconfig_servicetype_required',$id);
				$element->setParent(null);
				$this->_services[$id]=array($type,$properties->toArray(),$element);
				$this->_empty=false;
			}
			else
				throw new TConfigurationException('appconfig_services_invalid',$element->getTagName());
		}
	}

	/**
	 * Loads the parameters XML node.
	 * @param TXmlElement the parameters XML node
	 * @param string the context path (for specifying relative paths)
	 */
	protected function loadParametersXml($parametersNode,$configPath)
	{
		foreach($parametersNode->getElements() as $element)
		{
			if($element->getTagName()==='parameter')
			{
				$properties=$element->getAttributes();
				if(($id=$properties->remove('id'))===null)
					throw new TConfigurationException('appconfig_parameterid_required');
				if(($type=$properties->remove('class'))===null)
				{
					if(($value=$properties->remove('value'))===null)
						$this->_parameters[$id]=$element;
					else
						$this->_parameters[$id]=$value;
				}
				else
					$this->_parameters[$id]=array($type,$properties->toArray());
				$this->_empty=false;
			}
			else
				throw new TConfigurationException('appconfig_parameters_invalid',$element->getTagName());
		}
	}

	/**
	 * Loads the external XML configurations.
	 * @param TXmlElement the application DOM element
	 * @param string the context path (for specifying relative paths)
	 */
	protected function loadExternalXml($includeNode,$configPath)
	{
		if(($when=$includeNode->getAttribute('when'))===null)
			$when=true;
		if(($filePath=$includeNode->getAttribute('file'))===null)
			throw new TConfigurationException('appconfig_includefile_required');
		if(isset($this->_includes[$filePath]))
			$this->_includes[$filePath]='('.$this->_includes[$filePath].') || ('.$when.')';
		else
			$this->_includes[$filePath]=$when;
		$this->_empty=false;
	}

	/**
	 * Returns list of page initial property values.
	 * Each array element represents a single property with the key
	 * being the property name and the value the initial property value.
	 * @return array list of page initial property values
	 */
	public function getProperties()
	{
		return $this->_properties;
	}

	/**
	 * Returns list of path alias definitions.
	 * The definitions are aggregated (top-down) from configuration files along the path
	 * to the specified page. Each array element represents a single alias definition,
	 * with the key being the alias name and the value the absolute path.
	 * @return array list of path alias definitions
	 */
	public function getAliases()
	{
		return $this->_aliases;
	}

	/**
	 * Returns list of namespaces to be used.
	 * The namespaces are aggregated (top-down) from configuration files along the path
	 * to the specified page. Each array element represents a single namespace usage,
	 * with the value being the namespace to be used.
	 * @return array list of namespaces to be used
	 */
	public function getUsings()
	{
		return $this->_usings;
	}

	/**
	 * Returns list of module configurations.
	 * The module configurations are aggregated (top-down) from configuration files
	 * along the path to the specified page. Each array element represents
	 * a single module configuration, with the key being the module ID and
	 * the value the module configuration. Each module configuration is
	 * stored in terms of an array with the following content
	 * ([0]=>module type, [1]=>module properties, [2]=>complete module configuration)
	 * The module properties are an array of property values indexed by property names.
	 * The complete module configuration is a TXmlElement object representing
	 * the raw module configuration which may contain contents enclosed within
	 * module tags.
	 * @return array list of module configurations to be used
	 */
	public function getModules()
	{
		return $this->_modules;
	}

	/**
	 * @return array list of service configurations
	 */
	public function getServices()
	{
		return $this->_services;
	}

	/**
	 * Returns list of parameter definitions.
	 * The parameter definitions are aggregated (top-down) from configuration files
	 * along the path to the specified page. Each array element represents
	 * a single parameter definition, with the key being the parameter ID and
	 * the value the parameter definition. A parameter definition can be either
	 * a string representing a string-typed parameter, or an array.
	 * The latter defines a component-typed parameter whose format is as follows,
	 * ([0]=>component type, [1]=>component properties)
	 * The component properties are an array of property values indexed by property names.
	 * @return array list of parameter definitions to be used
	 */
	public function getParameters()
	{
		return $this->_parameters;
	}

	/**
	 * @return array list of external configuration files. Each element is like $filePath=>$condition
	 */
	public function getExternalConfigurations()
	{
		return $this->_includes;
	}
}

/**
 * TApplicationStatePersister class.
 * TApplicationStatePersister provides a file-based persistent storage
 * for application state. Application state, when serialized, is stored
 * in a file named 'global.cache' under the 'runtime' directory of the application.
 * Cache will be exploited if it is enabled.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TApplication.php 2704 2009-09-15 08:23:47Z Christophe.Boulain $
 * @package System
 * @since 3.0
 */
class TApplicationStatePersister extends TModule implements IStatePersister
{
	/**
	 * Name of the value stored in cache
	 */
	const CACHE_NAME='prado:appstate';

	/**
	 * Initializes module.
	 * @param TXmlElement module configuration (may be null)
	 */
	public function init($config)
	{
		$this->getApplication()->setApplicationStatePersister($this);
	}

	/**
	 * @return string the file path storing the application state
	 */
	protected function getStateFilePath()
	{
		return $this->getApplication()->getRuntimePath().'/global.cache';
	}

	/**
	 * Loads application state from persistent storage.
	 * @return mixed application state
	 */
	public function load()
	{
		if(($cache=$this->getApplication()->getCache())!==null && ($value=$cache->get(self::CACHE_NAME))!==false)
			return unserialize($value);
		else
		{
			if(($content=@file_get_contents($this->getStateFilePath()))!==false)
				return unserialize($content);
			else
				return null;
		}
	}

	/**
	 * Saves application state in persistent storage.
	 * @param mixed application state
	 */
	public function save($state)
	{
		$content=serialize($state);
		$saveFile=true;
		if(($cache=$this->getApplication()->getCache())!==null)
		{
			if($cache->get(self::CACHE_NAME)===$content)
				$saveFile=false;
			else
				$cache->set(self::CACHE_NAME,$content);
		}
		if($saveFile)
		{
			$fileName=$this->getStateFilePath();
			file_put_contents($fileName,$content,LOCK_EX);
		}
	}

}
