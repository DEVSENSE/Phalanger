<?php
/**
 * TJsonService and TJsonResponse class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TJsonService.php 2574 2008-11-30 23:09:51Z carlgmathisen $
 * @package System.Web.Services
 */

/**
 * TJsonService class provides to end-users javascript content response in
 * JSON format.
 *
 * TJsonService manages a set of {@link TJsonResponse}, each
 * representing specific response with javascript content.
 * The service parameter, referring to the ID of the service, specifies
 * which javascript content to be provided to end-users.
 *
 * To use TJsonService, configure it in application configuration as follows,
 * <code>
 *  <service id="json" class="System.Web.Services.TJsonService">
 *    <json id="get_article" class="Path.To.JsonResponseClass1" .../>
 *    <json id="register_rating" class="Path.To.JsonResponseClass2" .../>
 *  </service>
 * </code>
 * where each JSON response is specified via a &lt;json&gt; element.
 * Initial property values can be configured in a &lt;json&gt; element.
 *
 * To retrieve the JSON content provided by "get_article", use the URL
 * <code>index.php?json=get_article</code>
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TJsonService.php 2574 2008-11-30 23:09:51Z carlgmathisen $
 * @package System.Web.Services
 * @since 3.1
 */
class TJsonService extends TService
{
	/**
	 * @var array registered services
	 */
	private $_services=array();

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface.
	 * @param TXmlElement configuration for this module, can be null
	 */
	public function init($xml)
	{
		$this->loadJsonServices($xml);
	}

	/**
	 * Load the service definitions.
	 * @param TXmlElement configuration for this module, can be null
	 */
	protected function loadJsonServices($xml)
	{
		foreach($xml->getElementsByTagName('json') as $config)
		{
			if(($id=$config->getAttribute('id'))!==null)
				$this->_services[$id]=$config;
			else
				throw new TConfigurationException('jsonservice_id_required');
		}
	}

	/**
	 * Runs the service.
	 * This method is invoked by application automatically.
	 */
	public function run()
	{
		$id=$this->getRequest()->getServiceParameter();
		if(isset($this->_services[$id]))
		{
			$serviceConfig=$this->_services[$id];
			$properties=$serviceConfig->getAttributes();
			if(($class=$properties->remove('class'))!==null)
			{
				$service=Prado::createComponent($class);
				if($service instanceof TJsonResponse)
					$this->createJsonResponse($service,$properties,$serviceConfig);
				else
					throw new TConfigurationException('jsonservice_response_type_invalid',$id);
			}
			else
				throw new TConfigurationException('jsonservice_class_required',$id);
		}
		else
			throw new THttpException(404,'jsonservice_provider_unknown',$id);
	}

	/**
	 * Renders content provided by TJsonResponse::getJsonContent() as
	 * javascript in JSON format.
	 */
	protected function createJsonResponse($service,$properties,$config)
	{
		// init service properties
		foreach($properties as $name=>$value)
			$service->setSubproperty($name,$value);
		$service->init($config);

		//send content if not null
		if(($content=$service->getJsonContent())!==null)
		{
			$response = $this->getResponse();
			$response->setContentType('text/javascript');
			$response->setCharset('UTF-8');
			$json = Prado::createComponent('System.Web.Javascripts.TJSON');

			//send content
			$response->write($json->encode($content));
		}
	}
}

/**
 * TJsonResponse Class
 *
 * TJsonResponse is the base class for all JSON response provider classes.
 *
 * Derived classes must implement {@link getJsonContent()} to return
 * an object or literals to be converted to JSON format. The response
 * will be empty if the returned content is null.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TJsonService.php 2574 2008-11-30 23:09:51Z carlgmathisen $
 * @package System.Web.Services
 * @since 3.1
 */
abstract class TJsonResponse extends TApplicationComponent
{
	private $_id='';

	/**
	 * Initializes the feed.
	 * @param TXmlElement configurations specified in {@link TJsonService}.
	 */
	public function init($config)
	{
	}

	/**
	 * @return string ID of this response
	 */
	public function getID()
	{
		return $this->_id;
	}

	/**
	 * @param string ID of this response
	 */
	public function setID($value)
	{
		$this->_id=$value;
	}

	/**
	 * @return object json response content, null to suppress output.
	 */
	abstract public function getJsonContent();
}

?>
