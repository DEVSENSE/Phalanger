<?php
/**
 * TPageService class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPageService.php 2737 2009-11-08 07:33:48Z godzilla80@gmx.net $
 * @package System.Web.Services
 */

/**
 * Include classes to be used by page service
 */
Prado::using('System.Web.UI.TPage');
Prado::using('System.Web.UI.TTemplateManager');
Prado::using('System.Web.UI.TThemeManager');

/**
 * TPageService class.
 *
 * TPageService implements the service for serving user page requests.
 *
 * Pages that are available to client users are stored under a directory specified by
 * {@link setBasePath BasePath}. The directory may contain subdirectories.
 * Pages serving for a similar goal are usually placed under the same directory.
 * A directory may contain a configuration file <b>config.xml</b> whose content
 * is similar to that of application configuration file.
 *
 * A page is requested via page path, which is a dot-connected directory names
 * appended by the page name. Assume '<BasePath>/Users/Admin' is the directory
 * containing the page 'Update'. Then the page can be requested via 'Users.Admin.Update'.
 * By default, the {@link setBasePath BasePath} of the page service is the "pages"
 * directory under the application base path. You may change this default
 * by setting {@link setBasePath BasePath} with a different path you prefer.
 *
 * Page name refers to the file name (without extension) of the page template.
 * In order to differentiate from the common control template files, the extension
 * name of the page template files must be '.page'. If there is a PHP file with
 * the same page name under the same directory as the template file, that file
 * will be considered as the page class file and the file name is the page class name.
 * If such a file is not found, the page class is assumed as {@link TPage}.
 *
 * Modules can be configured and loaded in page directory configurations.
 * Configuration of a module in a subdirectory will overwrite its parent
 * directory's configuration, if both configurations refer to the same module.
 *
 * By default, TPageService will automatically load two modules:
 * - {@link TTemplateManager} : manages page and control templates
 * - {@link TThemeManager} : manages themes used in a Prado application
 *
 * In page directory configurations, static authorization rules can also be specified,
 * which governs who and which roles can access particular pages.
 * Refer to {@link TAuthorizationRule} for more details about authorization rules.
 * Page authorization rules can be configured within an <authorization> tag in
 * each page directory configuration as follows,
 * <authorization>
 *   <deny pages="Update" users="?" />
 *   <allow pages="Admin" roles="administrator" />
 *   <deny pages="Admin" users="*" />
 * </authorization>
 * where the 'pages' attribute may be filled with a sequence of comma-separated
 * page IDs. If 'pages' attribute does not appear in a rule, the rule will be
 * applied to all pages in this directory and all subdirectories (recursively).
 * Application of authorization rules are in a bottom-up fashion, starting from
 * the directory containing the requested page up to all parent directories.
 * The first matching rule will be used. The last rule always allows all users
 * accessing to any resources.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TPageService.php 2737 2009-11-08 07:33:48Z godzilla80@gmx.net $
 * @package System.Web.Services
 * @since 3.0
 */
class TPageService extends TService
{
	/**
	 * Configuration file name
	 */
	const CONFIG_FILE='config.xml';
	/**
	 * Default base path
	 */
	const DEFAULT_BASEPATH='pages';
	/**
	 * Prefix of ID used for storing parsed configuration in cache
	 */
	const CONFIG_CACHE_PREFIX='prado:pageservice:';
	/**
	 * Page template file extension
	 */
	const PAGE_FILE_EXT='.page';
	/**
	 * @var string root path of pages
	 */
	private $_basePath=null;
	/**
	 * @var string base path class in namespace format
	 */
	private $_basePageClass='TPage';
	/**
	 * @var string clientscript manager class in namespace format
	 * @since 3.1.7
	 */
	private $_clientScriptManagerClass='System.Web.UI.TClientScriptManager';
	/**
	 * @var string default page
	 */
	private $_defaultPage='Home';
	/**
	 * @var string requested page (path)
	 */
	private $_pagePath=null;
	/**
	 * @var TPage the requested page
	 */
	private $_page=null;
	/**
	 * @var array list of initial page property values
	 */
	private $_properties=array();
	/**
	 * @var boolean whether service is initialized
	 */
	private $_initialized=false;
	/**
	 * @var TThemeManager theme manager
	 */
	private $_themeManager=null;
	/**
	 * @var TTemplateManager template manager
	 */
	private $_templateManager=null;

	/**
	 * Initializes the service.
	 * This method is required by IService interface and is invoked by application.
	 * @param TXmlElement service configuration
	 */
	public function init($config)
	{
		Prado::trace("Initializing TPageService",'System.Web.Services.TPageService');

		$pageConfig=$this->loadPageConfig($config);

		$this->initPageContext($pageConfig);

		$this->_initialized=true;
	}

	/**
	 * Initializes page context.
	 * Page context includes path alias settings, namespace usages,
	 * parameter initialization, module loadings, page initial properties
	 * and authorization rules.
	 * @param TPageConfiguration
	 */
	protected function initPageContext($pageConfig)
	{
		$application=$this->getApplication();
		foreach($pageConfig->getApplicationConfigurations() as $appConfig)
			$application->applyConfiguration($appConfig);

		$this->applyConfiguration($pageConfig);
	}

	/**
	 * Applies a page configuration.
	 * @param TPageConfiguration the configuration
	 */
	protected function applyConfiguration($config)
	{
		// initial page properties (to be set when page runs)
		$this->_properties=array_merge($this->_properties, $config->getProperties());
		$this->getApplication()->getAuthorizationRules()->mergeWith($config->getRules());
		$pagePath=$this->getRequestedPagePath();
		// external configurations
		foreach($config->getExternalConfigurations() as $filePath=>$params)
		{
			list($configPagePath,$condition)=$params;
			if($condition!==true)
				$condition=$this->evaluateExpression($condition);
			if($condition)
			{
				if(($path=Prado::getPathOfNamespace($filePath,TApplication::CONFIG_FILE_EXT))===null || !is_file($path))
					throw new TConfigurationException('pageservice_includefile_invalid',$filePath);
				$c=new TPageConfiguration($pagePath);
				$c->loadFromFile($path,$configPagePath);
				$this->applyConfiguration($c);
			}
		}

	}

	/**
	 * Determines the requested page path.
	 * @return string page path requested
	 */
	protected function determineRequestedPagePath()
	{
		$pagePath=$this->getRequest()->getServiceParameter();
		if(empty($pagePath))
			$pagePath=$this->getDefaultPage();
		return $pagePath;
	}

	/**
	 * Collects configuration for a page.
	 * @param TXmlElement additional configuration specified in the application configuration
	 * @return TPageConfiguration
	 */
	protected function loadPageConfig($config)
	{
		$application=$this->getApplication();
		$pagePath=$this->getRequestedPagePath();
		if(($cache=$application->getCache())===null)
		{
			$pageConfig=new TPageConfiguration($pagePath);
			if($config!==null)
				$pageConfig->loadPageConfigurationFromXml($config,$application->getBasePath(),'');
			$pageConfig->loadFromFiles($this->getBasePath());
		}
		else
		{
			$configCached=true;
			$currentTimestamp=array();
			$arr=$cache->get(self::CONFIG_CACHE_PREFIX.$this->getID().$pagePath);
			if(is_array($arr))
			{
				list($pageConfig,$timestamps)=$arr;
				if($application->getMode()!==TApplicationMode::Performance)
				{
					foreach($timestamps as $fileName=>$timestamp)
					{
						if($fileName===0) // application config file
						{
							$appConfigFile=$application->getConfigurationFile();
							$currentTimestamp[0]=$appConfigFile===null?0:@filemtime($appConfigFile);
							if($currentTimestamp[0]>$timestamp || ($timestamp>0 && !$currentTimestamp[0]))
								$configCached=false;
						}
						else
						{
							$currentTimestamp[$fileName]=@filemtime($fileName);
							if($currentTimestamp[$fileName]>$timestamp || ($timestamp>0 && !$currentTimestamp[$fileName]))
								$configCached=false;
						}
					}
				}
			}
			else
			{
				$configCached=false;
				$paths=explode('.',$pagePath);
				$configPath=$this->getBasePath();
				foreach($paths as $path)
				{
					$configFile=$configPath.DIRECTORY_SEPARATOR.self::CONFIG_FILE;
					$currentTimestamp[$configFile]=@filemtime($configFile);
					$configPath.=DIRECTORY_SEPARATOR.$path;
				}
				$appConfigFile=$application->getConfigurationFile();
				$currentTimestamp[0]=$appConfigFile===null?0:@filemtime($appConfigFile);
			}
			if(!$configCached)
			{
				$pageConfig=new TPageConfiguration($pagePath);
				if($config!==null)
					$pageConfig->loadPageConfigurationFromXml($config,$application->getBasePath(),'');
				$pageConfig->loadFromFiles($this->getBasePath());
				$cache->set(self::CONFIG_CACHE_PREFIX.$this->getID().$pagePath,array($pageConfig,$currentTimestamp));
			}
		}
		return $pageConfig;
	}

	/**
	 * @return TTemplateManager template manager
	 */
	public function getTemplateManager()
	{
		if(!$this->_templateManager)
		{
			$this->_templateManager=new TTemplateManager;
			$this->_templateManager->init(null);
		}
		return $this->_templateManager;
	}

	/**
	 * @param TTemplateManager template manager
	 */
	public function setTemplateManager(TTemplateManager $value)
	{
		$this->_templateManager=$value;
	}

	/**
	 * @return TThemeManager theme manager
	 */
	public function getThemeManager()
	{
		if(!$this->_themeManager)
		{
			$this->_themeManager=new TThemeManager;
			$this->_themeManager->init(null);
		}
		return $this->_themeManager;
	}

	/**
	 * @param TThemeManager theme manager
	 */
	public function setThemeManager(TThemeManager $value)
	{
		$this->_themeManager=$value;
	}

	/**
	 * @return string the requested page path
	 */
	public function getRequestedPagePath()
	{
		if($this->_pagePath===null)
		{
			$this->_pagePath=strtr($this->determineRequestedPagePath(),'/\\','..');
			if(empty($this->_pagePath))
				throw new THttpException(404,'pageservice_page_required');
		}
		return $this->_pagePath;
	}

	/**
	 * @return TPage the requested page
	 */
	public function getRequestedPage()
	{
		return $this->_page;
	}

	/**
	 * @return string default page path to be served if no explicit page is request. Defaults to 'Home'.
	 */
	public function getDefaultPage()
	{
		return $this->_defaultPage;
	}

	/**
	 * @param string default page path to be served if no explicit page is request
	 * @throws TInvalidOperationException if the page service is initialized
	 */
	public function setDefaultPage($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('pageservice_defaultpage_unchangeable');
		else
			$this->_defaultPage=$value;
	}

	/**
	 * @return string the URL for the default page
	 */
	public function getDefaultPageUrl()
	{
		return $this->constructUrl($this->getDefaultPage());
	}

	/**
	 * @return string the root directory for storing pages. Defaults to the 'pages' directory under the application base path.
	 */
	public function getBasePath()
	{
		if($this->_basePath===null)
		{
			$basePath=$this->getApplication()->getBasePath().DIRECTORY_SEPARATOR.self::DEFAULT_BASEPATH;
			if(($this->_basePath=realpath($basePath))===false || !is_dir($this->_basePath))
				throw new TConfigurationException('pageservice_basepath_invalid',$basePath);
		}
		return $this->_basePath;
	}

	/**
	 * @param string root directory (in namespace form) storing pages
	 * @throws TInvalidOperationException if the service is initialized already or basepath is invalid
	 */
	public function setBasePath($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('pageservice_basepath_unchangeable');
		else if(($path=Prado::getPathOfNamespace($value))===null || !is_dir($path))
			throw new TConfigurationException('pageservice_basepath_invalid',$value);
		$this->_basePath=realpath($path);
	}

	/**
	 * Sets the base page class name (in namespace format).
	 * If a page only has a template file without page class file,
	 * this base page class will be instantiated.
	 * @param string class name
	 */
	public function setBasePageClass($value)
	{
		$this->_basePageClass=$value;
	}

	/**
	 * @return string base page class name in namespace format. Defaults to 'TPage'.
	 */
	public function getBasePageClass()
	{
		return $this->_basePageClass;
	}

	/**
	 * Sets the clientscript manager class (in namespace format).
	 * @param string class name
	 * @since 3.1.7
	 */
	public function setClientScriptManagerClass($value)
	{
		$this->_clientScriptManagerClass=$value;
	}

	/**
	 * @return string clientscript manager class in namespace format. Defaults to 'System.Web.UI.TClientScriptManager'.
	 * @since 3.1.7
	 */
	public function getClientScriptManagerClass()
	{
		return $this->_clientScriptManagerClass;
	}

	/**
	 * Runs the service.
	 * This will create the requested page, initializes it with the property values
	 * specified in the configuration, and executes the page.
	 */
	public function run()
	{
		Prado::trace("Running page service",'System.Web.Services.TPageService');
		$this->_page=$this->createPage($this->getRequestedPagePath());
		$this->runPage($this->_page,$this->_properties);
	}

	/**
	 * Creates a page instance based on requested page path.
	 * @param string requested page path
	 * @return TPage the requested page instance
	 * @throws THttpException if requested page path is invalid
	 * @throws TConfigurationException if the page class cannot be found
	 */
	protected function createPage($pagePath)
	{
		$path=$this->getBasePath().DIRECTORY_SEPARATOR.strtr($pagePath,'.',DIRECTORY_SEPARATOR);
		$hasTemplateFile=is_file($path.self::PAGE_FILE_EXT);
		$hasClassFile=is_file($path.Prado::CLASS_FILE_EXT);

		if(!$hasTemplateFile && !$hasClassFile)
			throw new THttpException(404,'pageservice_page_unknown',$pagePath);

		if($hasClassFile)
		{
			$className=basename($path);
			if(!class_exists($className,false))
				include_once($path.Prado::CLASS_FILE_EXT);
		}
		else
		{
			$className=$this->getBasePageClass();
			Prado::using($className);
			if(($pos=strrpos($className,'.'))!==false)
				$className=substr($className,$pos+1);
		}

 		if(!class_exists($className,false) || ($className!=='TPage' && !is_subclass_of($className,'TPage')))
			throw new THttpException(404,'pageservice_page_unknown',$pagePath);

		$page=new $className;
		$page->setPagePath($pagePath);

		if($hasTemplateFile)
			$page->setTemplate($this->getTemplateManager()->getTemplateByFileName($path.self::PAGE_FILE_EXT));

		return $page;
	}

	/**
	 * Executes a page.
	 * @param TPage the page instance to be run
	 * @param array list of initial page properties
	 */
	protected function runPage($page,$properties)
	{
		foreach($properties as $name=>$value)
			$page->setSubProperty($name,$value);
		$page->run($this->getResponse()->createHtmlWriter());
	}

	/**
	 * Constructs a URL with specified page path and GET parameters.
	 * @param string page path
	 * @param array list of GET parameters, null if no GET parameters required
	 * @param boolean whether to encode the ampersand in URL, defaults to true.
	 * @param boolean whether to encode the GET parameters (their names and values), defaults to true.
	 * @return string URL for the page and GET parameters
	 */
	public function constructUrl($pagePath,$getParams=null,$encodeAmpersand=true,$encodeGetItems=true)
	{
		return $this->getRequest()->constructUrl($this->getID(),$pagePath,$getParams,$encodeAmpersand,$encodeGetItems);
	}
}


/**
 * TPageConfiguration class
 *
 * TPageConfiguration represents the configuration for a page.
 * The page is specified by a dot-connected path.
 * Configurations along this path are merged together to be provided for the page.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TPageService.php 2737 2009-11-08 07:33:48Z godzilla80@gmx.net $
 * @package System.Web.Services
 * @since 3.0
 */
class TPageConfiguration extends TComponent
{
	/**
	 * @var array list of application configurations
	 */
	private $_appConfigs=array();
	/**
	 * @var array list of page initial property values
	 */
	private $_properties=array();
	/**
	 * @var TAuthorizationRuleCollection list of authorization rules
	 */
	private $_rules=array();
	/**
	 * @var array list of included configurations
	 */
	private $_includes=array();
	/**
	 * @var string the currently request page in the format of Path.To.PageName
	 */
	private $_pagePath='';

	/**
	 * Constructor.
	 * @param string the currently request page in the format of Path.To.PageName
	 */
	public function __construct($pagePath)
	{
		$this->_pagePath=$pagePath;
	}

	/**
	 * @return array list of external configuration files. Each element is like $filePath=>$condition
	 */
	public function getExternalConfigurations()
	{
		return $this->_includes;
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
	 * Returns list of authorization rules.
	 * The authorization rules are aggregated (bottom-up) from configuration files
	 * along the path to the specified page.
	 * @return TAuthorizationRuleCollection collection of authorization rules
	 */
	public function getRules()
	{
		return $this->_rules;
	}

	/**
	 * @return array list of application configurations specified along page path
	 */
	public function getApplicationConfigurations()
	{
		return $this->_appConfigs;
	}

	/**
	 * Loads configuration for a page specified in a path format.
	 * @param string root path for pages
	 */
	public function loadFromFiles($basePath)
	{
		$paths=explode('.',$this->_pagePath);
		$page=array_pop($paths);
		$path=$basePath;
		$configPagePath='';
		foreach($paths as $p)
		{
			$this->loadFromFile($path.DIRECTORY_SEPARATOR.TPageService::CONFIG_FILE,$configPagePath);
			$path.=DIRECTORY_SEPARATOR.$p;
			if($configPagePath==='')
				$configPagePath=$p;
			else
				$configPagePath.='.'.$p;
		}
		$this->loadFromFile($path.DIRECTORY_SEPARATOR.TPageService::CONFIG_FILE,$configPagePath);
		$this->_rules=new TAuthorizationRuleCollection($this->_rules);
	}

	/**
	 * Loads a specific config file.
	 * @param string config file name
	 * @param string the page path that the config file is associated with. The page path doesn't include the page name.
	 */
	public function loadFromFile($fname,$configPagePath)
	{
		Prado::trace("Loading page configuration file $fname",'System.Web.Services.TPageService');
		if(empty($fname) || !is_file($fname))
			return;
		$dom=new TXmlDocument;
		if($dom->loadFromFile($fname))
			$this->loadFromXml($dom,dirname($fname),$configPagePath);
		else
			throw new TConfigurationException('pageserviceconf_file_invalid',$fname);
	}

	/**
	 * Loads a page configuration.
	 * The configuration includes information for both application
	 * and page service.
	 * @param TXmlElement config xml element
	 * @param string the directory containing this configuration
	 * @param string the page path that the config XML is associated with. The page path doesn't include the page name.
	 */
	public function loadFromXml($dom,$configPath,$configPagePath)
	{
		$this->loadApplicationConfigurationFromXml($dom,$configPath);
		$this->loadPageConfigurationFromXml($dom,$configPath,$configPagePath);
	}

	/**
	 * Loads the configuration specific for application part
	 * @param TXmlElement config xml element
	 * @param string base path corresponding to this xml element
	 */
	public function loadApplicationConfigurationFromXml($dom,$configPath)
	{
		$appConfig=new TApplicationConfiguration;
		$appConfig->loadFromXml($dom,$configPath);
		$this->_appConfigs[]=$appConfig;
	}

	/**
	 * Loads the configuration specific for page service.
	 * @param TXmlElement config xml element
	 * @param string base path corresponding to this xml element
	 * @param string the page path that the config XML is associated with. The page path doesn't include the page name.
	 */
	public function loadPageConfigurationFromXml($dom,$configPath,$configPagePath)
	{
		// authorization
		if(($authorizationNode=$dom->getElementByTagName('authorization'))!==null)
		{
			$rules=array();
			foreach($authorizationNode->getElements() as $node)
			{
				$patterns=$node->getAttribute('pages');
				$ruleApplies=false;
				if(empty($patterns) || trim($patterns)==='*') // null or empty string
					$ruleApplies=true;
				else
				{
					foreach(explode(',',$patterns) as $pattern)
					{
						if(($pattern=trim($pattern))!=='')
						{
							// we know $configPagePath and $this->_pagePath
							if($configPagePath!=='')  // prepend the pattern with ConfigPagePath
								$pattern=$configPagePath.'.'.$pattern;
							if(strcasecmp($pattern,$this->_pagePath)===0)
							{
								$ruleApplies=true;
								break;
							}
							if($pattern[strlen($pattern)-1]==='*') // try wildcard matching
							{
								if(strncasecmp($this->_pagePath,$pattern,strlen($pattern)-1)===0)
								{
									$ruleApplies=true;
									break;
								}
							}
						}
					}
				}
				if($ruleApplies)
					$rules[]=new TAuthorizationRule($node->getTagName(),$node->getAttribute('users'),$node->getAttribute('roles'),$node->getAttribute('verb'),$node->getAttribute('ips'));
			}
			$this->_rules=array_merge($rules,$this->_rules);
		}

		// pages
		if(($pagesNode=$dom->getElementByTagName('pages'))!==null)
		{
			$this->_properties=array_merge($this->_properties,$pagesNode->getAttributes()->toArray());
			// at the page folder
			foreach($pagesNode->getElementsByTagName('page') as $node)
			{
				$properties=$node->getAttributes();
				$id=$properties->remove('id');
				if(empty($id))
					throw new TConfigurationException('pageserviceconf_page_invalid',$configPath);
				$matching=false;
				$id=($configPagePath==='')?$id:$configPagePath.'.'.$id;
				if(strcasecmp($id,$this->_pagePath)===0)
					$matching=true;
				else if($id[strlen($id)-1]==='*') // try wildcard matching
					$matching=strncasecmp($this->_pagePath,$id,strlen($id)-1)===0;
				if($matching)
					$this->_properties=array_merge($this->_properties,$properties->toArray());
			}
		}

		// external configurations
		foreach($dom->getElementsByTagName('include') as $node)
		{
			if(($when=$node->getAttribute('when'))===null)
				$when=true;
			if(($filePath=$node->getAttribute('file'))===null)
				throw new TConfigurationException('pageserviceconf_includefile_required');
			if(isset($this->_includes[$filePath]))
				$this->_includes[$filePath]=array($configPagePath,'('.$this->_includes[$filePath][1].') || ('.$when.')');
			else
				$this->_includes[$filePath]=array($configPagePath,$when);
		}
	}
}

