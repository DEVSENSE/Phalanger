<?php
/**
 * TSoapService and TSoapServer class file
 *
 * @author Knut Urdalen <knut.urdalen@gmail.com>
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSoapService.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.Services
 */

/**
 * TSoapService class
 *
 * TSoapService processes SOAP requests for a PRADO application.
 * TSoapService requires PHP SOAP extension to be loaded.
 *
 * TSoapService manages a set of SOAP providers. Each SOAP provider
 * is a class that implements a set of SOAP methods which are exposed
 * to SOAP clients for remote invocation. TSoapService generates WSDL
 * automatically for the SOAP providers by default.
 *
 * To use TSoapService, configure it in the application specification like following:
 * <code>
 *   <services>
 *     <service id="soap" class="System.Web.Services.TSoapService">
 *       <soap id="stockquote" provider="MyStockQuote" />
 *     </service>
 *   </services>
 * </code>
 *
 * The above example specifies a single SOAP provider named "stockquote"
 * whose class is "MyStockQuote". A SOAP client can then obtain the WSDL for
 * this provider via the following URL:
 * <code>
 *   http://hostname/path/to/index.php?soap=stockquote.wsdl
 * </code>
 *
 * The WSDL for the provider class "MyStockQuote" is generated based on special
 * comment tags in the class. In particular, if a class method's comment
 * contains the keyword "@soapmethod", it is considered to be a SOAP method
 * and will be exposed to SOAP clients. For example,
 * <code>
 *   class MyStockQuote {
 *      / **
 *       * @param string $symbol the stock symbol
 *       * @return float the stock price
 *       * @soapmethod
 *       * /
 *      public function getQuote($symbol) {...}
 *   }
 * </code>
 *
 * With the above SOAP provider, a typical SOAP client may call the method "getQuote"
 * remotely like the following:
 * <code>
 *   $client=new SoapClient("http://hostname/path/to/index.php?soap=stockquote.wsdl");
 *   echo $client->getQuote("ibm");
 * </code>
 *
 * Each <soap> element in the application specification actually configures
 * the properties of a SOAP server which defaults to {@link TSoapServer}.
 * Therefore, any writable property of {@link TSoapServer} may appear as an attribute
 * in the <soap> element. For example, the "provider" attribute refers to
 * the {@link TSoapServer::setProvider Provider} property of {@link TSoapServer}.
 * The following configuration specifies that the SOAP server is persistent within
 * the user session (that means a MyStockQuote object will be stored in session)
 * <code>
 *   <services>
 *     <service id="soap" class="System.Web.Services.TSoapService">
 *       <soap id="stockquote" provider="MyStockQuote" SessionPersistent="true" />
 *     </service>
 *   </services>
 * </code>
 *
 * You may also use your own SOAP server class by specifying the "class" attribute of <soap>.
 *
 * @author Knut Urdalen <knut.urdalen@gmail.com>
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @package System.Web.Services
 * @since 3.1
 */
class TSoapService extends TService
{
	const DEFAULT_SOAP_SERVER='TSoapServer';
	const CONFIG_FILE_EXT='.xml';
	private $_servers=array();
	private $_configFile=null;
	private $_wsdlRequest=false;
	private $_serverID=null;

	/**
	 * Constructor.
	 * Sets default service ID to 'soap'.
	 */
	public function __construct()
	{
		$this->setID('soap');
	}

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface.
	 * @param TXmlElement configuration for this module, can be null
	 * @throws TConfigurationException if {@link getConfigFile ConfigFile} is invalid.
	 */
	public function init($config)
	{
		if($this->_configFile!==null)
		{
 			if(is_file($this->_configFile))
 			{
				$dom=new TXmlDocument;
				$dom->loadFromFile($this->_configFile);
				$this->loadConfig($dom);
			}
			else
				throw new TConfigurationException('soapservice_configfile_invalid',$this->_configFile);
		}
		$this->loadConfig($config);

		$this->resolveRequest();
	}

	/**
	 * Resolves the request parameter.
	 * It identifies the server ID and whether the request is for WSDL.
	 * @throws THttpException if the server ID cannot be found
	 * @see getServerID
	 * @see getIsWsdlRequest
	 */
	protected function resolveRequest()
	{
		$serverID=$this->getRequest()->getServiceParameter();
		if(($pos=strrpos($serverID,'.wsdl'))===strlen($serverID)-5)
		{
			$serverID=substr($serverID,0,$pos);
			$this->_wsdlRequest=true;
		}
		else
			$this->_wsdlRequest=false;
		$this->_serverID=$serverID;
		if(!isset($this->_servers[$serverID]))
			throw new THttpException(400,'soapservice_request_invalid',$serverID);
	}

	/**
	 * Loads configuration from an XML element
	 * @param TXmlElement configuration node
	 * @throws TConfigurationException if soap server id is not specified or duplicated
	 */
	private function loadConfig($xml)
	{
		foreach($xml->getElementsByTagName('soap') as $serverXML)
		{
			$properties=$serverXML->getAttributes();
			if(($id=$properties->remove('id'))===null)
				throw new TConfigurationException('soapservice_serverid_required');
			if(isset($this->_servers[$id]))
				throw new TConfigurationException('soapservice_serverid_duplicated',$id);
			$this->_servers[$id]=$properties;
		}
	}

	/**
	 * @return string external configuration file. Defaults to null.
	 */
	public function getConfigFile()
	{
		return $this->_configFile;
	}

	/**
	 * @param string external configuration file in namespace format. The file
	 * must be suffixed with '.xml'.
	 * @throws TInvalidDataValueException if the file is invalid.
	 */
	public function setConfigFile($value)
	{
		if(($this->_configFile=Prado::getPathOfNamespace($value,self::CONFIG_FILE_EXT))===null)
			throw new TConfigurationException('soapservice_configfile_invalid',$value);
	}

	/**
	 * Constructs a URL with specified page path and GET parameters.
	 * @param string soap server ID
	 * @param array list of GET parameters, null if no GET parameters required
	 * @param boolean whether to encode the ampersand in URL, defaults to true.
	 * @param boolean whether to encode the GET parameters (their names and values), defaults to true.
	 * @return string URL for the page and GET parameters
	 */
	public function constructUrl($serverID,$getParams=null,$encodeAmpersand=true,$encodeGetItems=true)
	{
		return $this->getRequest()->constructUrl($this->getID(),$serverID,$getParams,$encodeAmpersand,$encodeGetItems);
	}

	/**
	 * @return boolean whether this is a request for WSDL
	 */
	public function getIsWsdlRequest()
	{
		return $this->_wsdlRequest;
	}

	/**
	 * @return string the SOAP server ID
	 */
	public function getServerID()
	{
		return $this->_serverID;
	}

	/**
	 * Creates the requested SOAP server.
	 * The SOAP server is initialized with the property values specified
	 * in the configuration.
	 * @return TSoapServer the SOAP server instance
	 */
	protected function createServer()
	{
		$properties=$this->_servers[$this->_serverID];
		if(($serverClass=$properties->remove('class'))===null)
			$serverClass=self::DEFAULT_SOAP_SERVER;
		Prado::using($serverClass);
		$className=($pos=strrpos($serverClass,'.'))!==false?substr($serverClass,$pos+1):$serverClass;
		if($className!==self::DEFAULT_SOAP_SERVER && !is_subclass_of($className,self::DEFAULT_SOAP_SERVER))
			throw new TConfigurationException('soapservice_server_invalid',$serverClass);
		$server=new $className;
		$server->setID($this->_serverID);
		foreach($properties as $name=>$value)
			$server->setSubproperty($name,$value);
		return $server;
	}

	/**
	 * Runs the service.
	 * If the service parameter ends with '.wsdl', it will serve a WSDL file for
	 * the specified soap server.
	 * Otherwise, it will handle the soap request using the specified server.
	 */
	public function run()
	{
		Prado::trace("Running SOAP service",'System.Web.Services.TSoapService');
		$server=$this->createServer();
		$this->getResponse()->setContentType('text/xml');
		$this->getResponse()->setCharset($server->getEncoding());
		if($this->getIsWsdlRequest())
		{
			// server WSDL file
			Prado::trace("Generating WSDL",'System.Web.Services.TSoapService');
			$this->getResponse()->write($server->getWsdl());
		}
		else
		{
			// provide SOAP service
			Prado::trace("Handling SOAP request",'System.Web.Services.TSoapService');
			$server->run();
		}
	}
}


/**
 * TSoapServer class.
 *
 * TSoapServer is a wrapper of the PHP SoapServer class.
 * It associates a SOAP provider class to the SoapServer object.
 * It also manages the URI for the SOAP service and WSDL.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TSoapService.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.Services
 * @since 3.1
 */
class TSoapServer extends TApplicationComponent
{
	const WSDL_CACHE_PREFIX='wsdl.';

	private $_id;
	private $_provider;

	private $_version='';
	private $_actor='';
	private $_encoding='';
	private $_uri='';
	private $_classMap;
	private $_persistent=false;
	private $_wsdlUri='';

	private $_requestedMethod;

	private $_server;

	/**
	 * @return string the ID of the SOAP server
	 */
	public function getID()
	{
		return $this->_id;
	}

	/**
	 * @param string the ID of the SOAP server
	 * @throws TInvalidDataValueException if the ID ends with '.wsdl'.
	 */
	public function setID($id)
	{
		if(strrpos($this->_id,'.wsdl')===strlen($this->_id)-5)
			throw new TInvalidDataValueException('soapserver_id_invalid',$id);
		$this->_id=$id;
	}

	/**
	 * Handles the SOAP request.
	 */
	public function run()
	{
		if(($provider=$this->getProvider())!==null)
		{
			Prado::using($provider);
			$providerClass=($pos=strrpos($provider,'.'))!==false?substr($provider,$pos+1):$provider;
			$this->guessMethodCallRequested($providerClass);
			$server=$this->createServer();
			$server->setClass($providerClass, $this);
			if($this->_persistent)
				$server->setPersistence(SOAP_PERSISTENCE_SESSION);
		}
		else
			$server=$this->createServer();
		try
		{
			$server->handle();
		}
		catch (Exception $e)
		{
			if($this->getApplication()->getMode()===TApplicationMode::Debug)
				$this->fault($e->getMessage(), $e->__toString());
			else
				$this->fault($e->getMessage());
		}
	}

	/**
	 * Generate a SOAP fault message.
	 * @param string message title
	 * @param mixed message details
	 * @param string message code, defalt is 'SERVER'.
	 * @param string actors
	 * @param string message name
	 */
	public function fault($title, $details='', $code='SERVER', $actor='', $name='')
	{
		Prado::trace('SOAP-Fault '.$code. ' '.$title.' : '.$details, 'System.Web.Services.TSoapService');
		$this->_server->fault($code, $title, $actor, $details, $name);
	}

	/**
	 * Guess the SOAP method request from the actual SOAP message
	 *
	 * @param string $class current handler class.
	 */
	protected function guessMethodCallRequested($class)
	{
		$namespace = $class.'wsdl';
		$message = file_get_contents("php://input");
		$matches= array();
		if(preg_match('/xmlns:([^=]+)="urn:'.$namespace.'"/', $message, $matches))
		{
			if(preg_match('/<'.$matches[1].':([a-zA-Z_]+[a-zA-Z0-9_]+)/', $message, $method))
			{
				$this->_requestedMethod = $method[1];
			}
		}
	}

	/**
	 * Soap method guessed from the SOAP message received.
	 * @return string soap method request, null if not found.
	 */
	public function getRequestedMethod()
	{
		return $this->_requestedMethod;
	}

	/**
	 * Creates the SoapServer instance.
	 * @return SoapServer
	 */
	protected function createServer()
	{
		if($this->_server===null)
		{
			if($this->getApplication()->getMode()===TApplicationMode::Debug)
				ini_set("soap.wsdl_cache_enabled",0);
			$this->_server = new SoapServer($this->getWsdlUri(),$this->getOptions());
		}
		return $this->_server;
	}

	/**
	 * @return array options for creating SoapServer instance
	 */
	protected function getOptions()
	{
		$options=array();
		if($this->_version==='1.1')
			$options['soap_version']=SOAP_1_1;
		else if($this->_version==='1.2')
			$options['soap_version']=SOAP_1_2;
		if(!empty($this->_actor))
			$options['actor']=$this->_actor;
		if(!empty($this->_encoding))
			$options['encoding']=$this->_encoding;
		if(!empty($this->_uri))
			$options['uri']=$this->_uri;
		if(is_string($this->_classMap))
		{
			foreach(preg_split('/\s*,\s*/', $this->_classMap) as $className)
				$options['classmap'][$className]=$className; //complex type uses the class name in the wsdl
		}
		return $options;
	}

	/**
	 * Returns the WSDL content of the SOAP server.
	 * If {@link getWsdlUri WsdlUri} is set, its content will be returned.
	 * If not, the {@link setProvider Provider} class will be investigated
	 * and the WSDL will be automatically genearted.
	 * @return string the WSDL content of the SOAP server
	 */
	public function getWsdl()
	{
		if($this->_wsdlUri==='')
		{
			$provider=$this->getProvider();
			$providerClass=($pos=strrpos($provider,'.'))!==false?substr($provider,$pos+1):$provider;
			Prado::using($provider);
			if($this->getApplication()->getMode()===TApplicationMode::Performance && ($cache=$this->getApplication()->getCache())!==null)
			{
				$wsdl=$cache->get(self::WSDL_CACHE_PREFIX.$providerClass);
				if(is_string($wsdl))
					return $wsdl;
				Prado::using('System.3rdParty.WsdlGen.WsdlGenerator');
				$wsdl=WsdlGenerator::generate($providerClass, $this->getUri(), $this->getEncoding());
				$cache->set(self::WSDL_CACHE_PREFIX.$providerClass,$wsdl);
				return $wsdl;
			}
			else
			{
				Prado::using('System.3rdParty.WsdlGen.WsdlGenerator');
				return WsdlGenerator::generate($providerClass, $this->getUri(), $this->getEncoding());
			}
		}
		else
			return file_get_contents($this->_wsdlUri);
	}

	/**
	 * @return string the URI for WSDL
	 */
	public function getWsdlUri()
	{
		if($this->_wsdlUri==='')
			return $this->getRequest()->getBaseUrl().$this->getService()->constructUrl($this->getID().'.wsdl',false);
		else
			return $this->_wsdlUri;
	}

	/**
	 * @param string the URI for WSDL
	 */
	public function setWsdlUri($value)
	{
		$this->_wsdlUri=$value;
	}

	/**
	 * @return string the URI for the SOAP service
	 */
	public function getUri()
	{
		if($this->_uri==='')
			return $this->getRequest()->getBaseUrl().$this->getService()->constructUrl($this->getID(),false);
		else
			return $this->_uri;
	}

	/**
	 * @param string the URI for the SOAP service
	 */
	public function setUri($uri)
	{
		$this->_uri=$uri;
	}

	/**
	 * @return string the SOAP provider class (in namespace format)
	 */
	public function getProvider()
	{
		return $this->_provider;
	}

	/**
	 * @param string the SOAP provider class (in namespace format)
	 */
	public function setProvider($provider)
	{
		$this->_provider=$provider;
	}

	/**
	 * @return string SOAP version, defaults to empty (meaning not set).
	 */
	public function getVersion()
	{
		return $this->_version;
	}

	/**
	 * @param string SOAP version, either '1.1' or '1.2'
	 * @throws TInvalidDataValueException if neither '1.1' nor '1.2'
	 */
	public function setVersion($value)
	{
		if($value==='1.1' || $value==='1.2' || $value==='')
			$this->_version=$value;
		else
			throw new TInvalidDataValueException('soapserver_version_invalid',$value);
	}

	/**
	 * @return string actor of the SOAP service
	 */
	public function getActor()
	{
		return $this->_actor;
	}

	/**
	 * @param string actor of the SOAP service
	 */
	public function setActor($value)
	{
		$this->_actor=$value;
	}

	/**
	 * @return string encoding of the SOAP service
	 */
	public function getEncoding()
	{
		return $this->_encoding;
	}

	/**
	 * @param string encoding of the SOAP service
	 */
	public function setEncoding($value)
	{
		$this->_encoding=$value;
	}

	/**
	 * @return boolean whether the SOAP service is persistent within session. Defaults to false.
	 */
	public function getSessionPersistent()
	{
		return $this->_persistent;
	}

	/**
	 * @param boolean whether the SOAP service is persistent within session.
	 */
	public function setSessionPersistent($value)
	{
		$this->_persistent=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return string comma delimit list of complex type classes.
	 */
	public function getClassMaps()
	{
		return $this->_classMap;
	}

	/**
	 * @return string comma delimit list of class names
	 */
	public function setClassMaps($classes)
	{
		$this->_classMap = $classes;
	}
}

