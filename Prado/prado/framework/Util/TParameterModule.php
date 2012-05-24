<?php
/**
 * TParameterModule class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TParameterModule.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Util
 */

/**
 * TParameterModule class
 *
 * TParameterModule enables loading application parameters from external
 * storage other than the application configuration.
 * To load parameters from an XML file, configure the module by setting
 * its {@link setParameterFile ParameterFile} property.
 * Note, the property only accepts a file path in namespace format with
 * file extension being '.xml'. The file format is as follows,  which is
 * similar to the parameter portion in an application configuration,
 * <code>
 * <parameters>
 *   <parameter id="param1" value="paramValue1" />
 *   <parameter id="param2" Property1="Value1" Property2="Value2" ... />
 * </parameters>
 * </code>
 *
 * In addition, any content enclosed within the module tag is also treated
 * as parameters, e.g.,
 * <code>
 * <module class="System.Util.TParameterModule">
 *   <parameter id="param1" value="paramValue1" />
 *   <parameter id="param2" Property1="Value1" Property2="Value2" ... />
 * </module>
 * </code>
 *
 * If a parameter is defined both in the external file and within the module
 * tag, the former takes precedence.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TParameterModule.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Util
 * @since 3.0
 */
class TParameterModule extends TModule
{
	const PARAM_FILE_EXT='.xml';
	private $_initialized=false;
	private $_paramFile=null;

	/**
	 * Initializes the module by loading parameters.
	 * @param TXmlElement content enclosed within the module tag
	 */
	public function init($config)
	{
		$this->loadParameters($config);
		if($this->_paramFile!==null)
		{
			if(($cache=$this->getApplication()->getCache())!==null)
			{
				$cacheKey='TParameterModule:'.$this->_paramFile;
				if(($dom=$cache->get($cacheKey))===false)
				{
					$dom=new TXmlDocument;
					$dom->loadFromFile($this->_paramFile);
					$cache->set($cacheKey,$dom,0,new TFileCacheDependency($this->_paramFile));
				}
			}
			else
			{
				$dom=new TXmlDocument;
				$dom->loadFromFile($this->_paramFile);
			}
			$this->loadParameters($dom);
		}
		$this->_initialized=true;
	}

	/**
	 * Loads parameters into application.
	 * @param TXmlElement XML representation of the parameters
	 * @throws TConfigurationException if the parameter file format is invalid
	 */
	protected function loadParameters($xmlNode)
	{
		$parameters=array();
		foreach($xmlNode->getElementsByTagName('parameter') as $node)
		{
			$properties=$node->getAttributes();
			if(($id=$properties->remove('id'))===null)
				throw new TConfigurationException('parametermodule_parameterid_required');
			if(($type=$properties->remove('class'))===null)
			{
				if(($value=$properties->remove('value'))===null)
					$parameters[$id]=$node;
				else
					$parameters[$id]=$value;
			}
			else
				$parameters[$id]=array($type,$properties->toArray());
		}

		$appParams=$this->getApplication()->getParameters();
		foreach($parameters as $id=>$parameter)
		{
			if(is_array($parameter))
			{
				$component=Prado::createComponent($parameter[0]);
				foreach($parameter[1] as $name=>$value)
					$component->setSubProperty($name,$value);
				$appParams->add($id,$component);
			}
			else
				$appParams->add($id,$parameter);
		}
	}

	/**
	 * @return string the parameter file path
	 */
	public function getParameterFile()
	{
		return $this->_paramFile;
	}

	/**
	 * @param string the parameter file path. It must be in namespace format
	 * and the file extension is '.xml'.
	 * @throws TInvalidOperationException if the module is initialized
	 * @throws TConfigurationException if the file is invalid
	 */
	public function setParameterFile($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('parametermodule_parameterfile_unchangeable');
		else if(($this->_paramFile=Prado::getPathOfNamespace($value,self::PARAM_FILE_EXT))===null || !is_file($this->_paramFile))
			throw new TConfigurationException('parametermodule_parameterfile_invalid',$value);
	}
}

