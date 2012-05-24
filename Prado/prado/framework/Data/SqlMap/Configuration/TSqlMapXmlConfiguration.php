<?php
/**
 * TSqlMapXmlConfigBuilder, TSqlMapXmlConfiguration, TSqlMapXmlMappingConfiguration classes file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqlMapXmlConfiguration.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.SqlMap.Configuration
 */

Prado::using('System.Data.SqlMap.Configuration.TSqlMapStatement');

/**
 * TSqlMapXmlConfig class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TSqlMapXmlConfiguration.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.SqlMap.Configuration
 */
abstract class TSqlMapXmlConfigBuilder
{
	/**
	 * Create an instance of an object give by the attribute named 'class' in the
	 * node and set the properties on the object given by attribute names and values.
	 * @param SimpleXmlNode property node
	 * @return Object new instance of class with class name given by 'class' attribute value.
	 */
	protected function createObjectFromNode($node)
	{
		if(isset($node['class']))
		{
			$obj = Prado::createComponent((string)$node['class']);
			$this->setObjectPropFromNode($obj,$node,array('class'));
			return $obj;
		}
		throw new TSqlMapConfigurationException(
			'sqlmap_node_class_undef', $node, $this->getConfigFile());
	}

	/**
	 * For each attributes (excluding attribute named in $except) set the
	 * property of the $obj given by the name of the attribute with the value
	 * of the attribute.
	 * @param Object object instance
	 * @param SimpleXmlNode property node
	 * @param array exception property name
	 */
	protected function setObjectPropFromNode($obj,$node,$except=array())
	{
		foreach($node->attributes() as $name=>$value)
		{
			if(!in_array($name,$except))
			{
				if($obj->canSetProperty($name))
					$obj->{$name} = (string)$value;
				else
					throw new TSqlMapConfigurationException(
						'sqlmap_invalid_property', $name, get_class($obj),
						$node, $this->getConfigFile());
			}
		}
	}

	/**
	 * Gets the filename relative to the basefile.
	 * @param string base filename
	 * @param string relative filename
	 * @return string absolute filename.
	 */
	protected function getAbsoluteFilePath($basefile,$resource)
	{
		$basedir = dirname($basefile);
		$file = realpath($basedir.DIRECTORY_SEPARATOR.$resource);
		if(!is_string($file) || !is_file($file))
			$file = realpath($resource);
		if(is_string($file) && is_file($file))
			return $file;
		else
			throw new TSqlMapConfigurationException(
				'sqlmap_unable_to_find_resource', $resource);
	}

	/**
	 * Load document using simple xml.
	 * @param string filename.
	 * @return SimpleXmlElement xml document.
	 */
	protected function loadXmlDocument($filename,TSqlMapXmlConfiguration $config)
	{
		if( strpos($filename, '${') !== false)
			$filename = $config->replaceProperties($filename);

		if(!is_file($filename))
			throw new TSqlMapConfigurationException(
				'sqlmap_unable_to_find_config', $filename);
		return simplexml_load_string($config->replaceProperties(file_get_contents($filename)));
	}

	/**
	 * Get element node by ID value (try for attribute name ID as case insensitive).
	 * @param SimpleXmlDocument $document
	 * @param string tag name.
	 * @param string id value.
	 * @return SimpleXmlElement node if found, null otherwise.
	 */
	protected function getElementByIdValue($document, $tag, $value)
	{
		//hack to allow upper case and lower case attribute names.
		foreach(array('id','ID','Id', 'iD') as $id)
		{
			$xpath = "//{$tag}[@{$id}='{$value}']";
			foreach($document->xpath($xpath) as $node)
				return $node;
		}
	}

	/**
	 * @return string configuration file.
	 */
	protected abstract function getConfigFile();
}

/**
 * TSqlMapXmlConfig class.
 *
 * Configures the TSqlMapManager using xml configuration file.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapXmlConfiguration.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapXmlConfiguration extends TSqlMapXmlConfigBuilder
{
	/**
	 * @var TSqlMapManager manager
	 */
	private $_manager;
	/**
	 * @var string configuration file.
	 */
	private $_configFile;
	/**
	 * @var array global properties.
	 */
	private $_properties=array();

	/**
	 * @param TSqlMapManager manager instance.
	 */
	public function __construct($manager)
	{
		$this->_manager=$manager;
	}

	public function getManager()
	{
		return $this->_manager;
	}

	protected function getConfigFile()
	{
		return $this->_configFile;
	}

	/**
	 * Configure the TSqlMapManager using the given xml file.
	 * @param string SqlMap configuration xml file.
	 */
	public function configure($filename=null)
	{
		$this->_configFile=$filename;
		$document = $this->loadXmlDocument($filename,$this);

		foreach($document->xpath('//property') as $property)
			$this->loadGlobalProperty($property);

		foreach($document->xpath('//typeHandler') as $handler)
			$this->loadTypeHandler($handler);

		foreach($document->xpath('//connection[last()]') as $conn)
			$this->loadDatabaseConnection($conn);

		//try to load configuration in the current config file.
		$mapping = new TSqlMapXmlMappingConfiguration($this);
		$mapping->configure($filename);

		foreach($document->xpath('//sqlMap') as $sqlmap)
			$this->loadSqlMappingFiles($sqlmap);

		$this->resolveResultMapping();
		$this->attachCacheModels();
	}

	/**
	 * Load global replacement property.
	 * @param SimpleXmlElement property node.
	 */
	protected function loadGlobalProperty($node)
	{
		$this->_properties[(string)$node['name']] = (string)$node['value'];
	}

	/**
	 * Load the type handler configurations.
	 * @param SimpleXmlElement type handler node
	 */
	protected function loadTypeHandler($node)
	{
		$handler = $this->createObjectFromNode($node);
		$this->_manager->getTypeHandlers()->registerTypeHandler($handler);
	}

	/**
	 * Load the database connection tag.
	 * @param SimpleXmlElement connection node.
	 */
	protected function loadDatabaseConnection($node)
	{
		$conn = $this->createObjectFromNode($node);
		$this->_manager->setDbConnection($conn);
	}

	/**
	 * Load SqlMap mapping configuration.
	 * @param unknown_type $node
	 */
	protected function loadSqlMappingFiles($node)
	{
		if(strlen($resource = (string)$node['resource']) > 0)
		{
			if( strpos($resource, '${') !== false)
				$resource = $this->replaceProperties($resource);

			$mapping = new TSqlMapXmlMappingConfiguration($this);
			$filename = $this->getAbsoluteFilePath($this->_configFile, $resource);
			$mapping->configure($filename);
		}
	}

	/**
	 * Resolve nest result mappings.
	 */
	protected function resolveResultMapping()
	{
		$maps = $this->_manager->getResultMaps();
		foreach($maps as $entry)
		{
			foreach($entry->getColumns() as $item)
			{
				$resultMap = $item->getResultMapping();
				if(strlen($resultMap) > 0)
				{
					if($maps->contains($resultMap))
						$item->setNestedResultMap($maps[$resultMap]);
					else
						throw new TSqlMapConfigurationException(
							'sqlmap_unable_to_find_result_mapping',
								$resultMap, $this->_configFile, $entry->getID());
				}
			}
			if($entry->getDiscriminator()!==null)
				$entry->getDiscriminator()->initialize($this->_manager);
		}
	}

	/**
	 * Set the cache for each statement having a cache model property.
	 */
	protected function attachCacheModels()
	{
		foreach($this->_manager->getMappedStatements() as $mappedStatement)
		{
			if(strlen($model = $mappedStatement->getStatement()->getCacheModel()) > 0)
			{
				$cache = $this->_manager->getCacheModel($model);
				$mappedStatement->getStatement()->setCache($cache);
			}
		}
	}

	/**
	 * Replace the place holders ${name} in text with properties the
	 * corresponding global property value.
	 * @param string original string.
	 * @return string string with global property replacement.
	 */
	public function replaceProperties($string)
	{
		foreach($this->_properties as $find => $replace)
			$string = str_replace('${'.$find.'}', $replace, $string);
		return $string;
	}
}

/**
 * Loads the statements, result maps, parameters maps from xml configuration.
 *
 * description
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapXmlConfiguration.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.SqlMap.Configuration
 * @since 3.1
 */
class TSqlMapXmlMappingConfiguration extends TSqlMapXmlConfigBuilder
{
	private $_xmlConfig;
	private $_configFile;
	private $_manager;

	private $_document;

	private $_FlushOnExecuteStatements=array();

	/**
	 * Regular expressions for escaping simple/inline parameter symbols
	 */
	const SIMPLE_MARK='$';
	const INLINE_SYMBOL='#';
	const ESCAPED_SIMPLE_MARK_REGEXP='/\$\$/';
	const ESCAPED_INLINE_SYMBOL_REGEXP='/\#\#/';
	const SIMPLE_PLACEHOLDER='`!!`';
	const INLINE_PLACEHOLDER='`!!!`';

	/**
	 * @param TSqlMapXmlConfiguration parent xml configuration.
	 */
	public function __construct(TSqlMapXmlConfiguration $xmlConfig)
	{
		$this->_xmlConfig=$xmlConfig;
		$this->_manager=$xmlConfig->getManager();
	}

	protected function getConfigFile()
	{
		return $this->_configFile;
	}

	/**
	 * Configure an XML mapping.
	 * @param string xml mapping filename.
	 */
	public function configure($filename)
	{
		$this->_configFile=$filename;
		$document = $this->loadXmlDocument($filename,$this->_xmlConfig);
		$this->_document=$document;

		static $bCacheDependencies;
		if($bCacheDependencies === null)
			$bCacheDependencies = Prado::getApplication()->getMode() !== TApplicationMode::Performance;

		if($bCacheDependencies)
			$this->_manager->getCacheDependencies()
					->getDependencies()
					->add(new TFileCacheDependency($filename));

		foreach($document->xpath('//resultMap') as $node)
			$this->loadResultMap($node);

		foreach($document->xpath('//parameterMap') as $node)
			$this->loadParameterMap($node);

		foreach($document->xpath('//statement') as $node)
			$this->loadStatementTag($node);

		foreach($document->xpath('//select') as $node)
			$this->loadSelectTag($node);

		foreach($document->xpath('//insert') as $node)
			$this->loadInsertTag($node);

		foreach($document->xpath('//update') as $node)
			$this->loadUpdateTag($node);

		foreach($document->xpath('//delete') as $node)
			$this->loadDeleteTag($node);

		foreach($document->xpath('//procedure') as $node)
			$this->loadProcedureTag($node);

		foreach($document->xpath('//cacheModel') as $node)
				$this->loadCacheModel($node);

		$this->registerCacheTriggers();
	}

	/**
	 * Load the result maps.
	 * @param SimpleXmlElement result map node.
	 */
	protected function loadResultMap($node)
	{
		$resultMap = $this->createResultMap($node);

		//find extended result map.
		if(strlen($extendMap = $resultMap->getExtends()) > 0)
		{
			if(!$this->_manager->getResultMaps()->contains($extendMap))
			{
				$extendNode=$this->getElementByIdValue($this->_document,'resultMap',$extendMap);
				if($extendNode!==null)
					$this->loadResultMap($extendNode);
			}

			if(!$this->_manager->getResultMaps()->contains($extendMap))
				throw new TSqlMapConfigurationException(
					'sqlmap_unable_to_find_parent_result_map', $node, $this->_configFile, $extendMap);

			$superMap = $this->_manager->getResultMap($extendMap);
			$resultMap->getColumns()->mergeWith($superMap->getColumns());
		}

		//add the result map
		if(!$this->_manager->getResultMaps()->contains($resultMap->getID()))
			$this->_manager->addResultMap($resultMap);
	}

	/**
	 * Create a new result map and its associated result properties,
	 * disciminiator and sub maps.
	 * @param SimpleXmlElement result map node
	 * @return TResultMap SqlMap result mapping.
	 */
	protected function createResultMap($node)
	{
		$resultMap = new TResultMap();
		$this->setObjectPropFromNode($resultMap,$node);

		//result nodes
		foreach($node->result as $result)
		{
			$property = new TResultProperty($resultMap);
			$this->setObjectPropFromNode($property,$result);
			$resultMap->addResultProperty($property);
		}

		//create the discriminator
		$discriminator = null;
		if(isset($node->discriminator))
		{
			$discriminator = new TDiscriminator();
			$this->setObjectPropFromNode($discriminator, $node->discriminator);
			$discriminator->initMapping($resultMap);
		}

		foreach($node->xpath('subMap') as $subMapNode)
		{
			if($discriminator===null)
				throw new TSqlMapConfigurationException(
					'sqlmap_undefined_discriminator', $node, $this->_configFile,$subMapNode);
			$subMap = new TSubMap;
			$this->setObjectPropFromNode($subMap,$subMapNode);
			$discriminator->addSubMap($subMap);
		}

		if($discriminator!==null)
			$resultMap->setDiscriminator($discriminator);

		return $resultMap;
	}

	/**
	 * Load parameter map from xml.
	 *
	 * @param SimpleXmlElement parameter map node.
	 */
	protected function loadParameterMap($node)
	{
		$parameterMap = $this->createParameterMap($node);

		if(strlen($extendMap = $parameterMap->getExtends()) > 0)
		{
			if(!$this->_manager->getParameterMaps()->contains($extendMap))
			{
				$extendNode=$this->getElementByIdValue($this->_document,'parameterMap',$extendMap);
				if($extendNode!==null)
					$this->loadParameterMap($extendNode);
			}

			if(!$this->_manager->getParameterMaps()->contains($extendMap))
				throw new TSqlMapConfigurationException(
					'sqlmap_unable_to_find_parent_parameter_map', $node, $this->_configFile,$extendMap);
			$superMap = $this->_manager->getParameterMap($extendMap);
			$index = 0;
			foreach($superMap->getPropertyNames() as $propertyName)
				$parameterMap->insertProperty($index++,$superMap->getProperty($propertyName));
		}
		$this->_manager->addParameterMap($parameterMap);
	}

	/**
	 * Create a new parameter map from xml node.
	 * @param SimpleXmlElement parameter map node.
	 * @return TParameterMap new parameter mapping.
	 */
	protected function createParameterMap($node)
	{
		$parameterMap = new TParameterMap();
		$this->setObjectPropFromNode($parameterMap,$node);
		foreach($node->parameter as $parameter)
		{
			$property = new TParameterProperty();
			$this->setObjectPropFromNode($property,$parameter);
			$parameterMap->addProperty($property);
		}
		return $parameterMap;
	}

	/**
	 * Load statement mapping from xml configuration file.
	 * @param SimpleXmlElement statement node.
	 */
	protected function loadStatementTag($node)
	{
		$statement = new TSqlMapStatement();
		$this->setObjectPropFromNode($statement,$node);
		$this->processSqlStatement($statement, $node);
		$mappedStatement = new TMappedStatement($this->_manager, $statement);
		$this->_manager->addMappedStatement($mappedStatement);
	}

	/**
	 * Load extended SQL statements if application. Replaces global properties
	 * in the sql text. Extracts inline parameter maps.
	 * @param TSqlMapStatement mapped statement.
	 * @param SimpleXmlElement statement node.
	 */
	protected function processSqlStatement($statement, $node)
	{
		$commandText = (string)$node;
		if(strlen($extend = $statement->getExtends()) > 0)
		{
			$superNode = $this->getElementByIdValue($this->_document,'*',$extend);
			if($superNode!==null)
				$commandText = (string)$superNode . $commandText;
			else
				throw new TSqlMapConfigurationException(
						'sqlmap_unable_to_find_parent_sql', $extend, $this->_configFile,$node);
		}
		//$commandText = $this->_xmlConfig->replaceProperties($commandText);
		$statement->initialize($this->_manager);
		$this->applyInlineParameterMap($statement, $commandText, $node);
	}

	/**
	 * Extract inline parameter maps.
	 * @param TSqlMapStatement statement object.
	 * @param string sql text
	 * @param SimpleXmlElement statement node.
	 */
	protected function applyInlineParameterMap($statement, $sqlStatement, $node)
	{
		$scope['file'] = $this->_configFile;
		$scope['node'] = $node;

		$sqlStatement=preg_replace(self::ESCAPED_INLINE_SYMBOL_REGEXP,self::INLINE_PLACEHOLDER,$sqlStatement);
		if($statement->parameterMap() === null)
		{
			// Build a Parametermap with the inline parameters.
			// if they exist. Then delete inline infos from sqltext.
			$parameterParser = new TInlineParameterMapParser;
			$sqlText = $parameterParser->parse($sqlStatement, $scope);
			if(count($sqlText['parameters']) > 0)
			{
				$map = new TParameterMap();
				$map->setID($statement->getID().'-InLineParameterMap');
				$statement->setInlineParameterMap($map);
				foreach($sqlText['parameters'] as $property)
					$map->addProperty($property);
			}
			$sqlStatement = $sqlText['sql'];
		}
		$sqlStatement=preg_replace('/'.self::INLINE_PLACEHOLDER.'/',self::INLINE_SYMBOL,$sqlStatement);

		$this->prepareSql($statement, $sqlStatement, $node);
	}

	/**
	 * Prepare the sql text (may extend to dynamic sql).
	 * @param TSqlMapStatement mapped statement.
	 * @param string sql text.
	 * @param SimpleXmlElement statement node.
	 * @todo Extend to dynamic sql.
	 */
	protected function prepareSql($statement,$sqlStatement, $node)
	{
		$simpleDynamic = new TSimpleDynamicParser;
		$sqlStatement=preg_replace(self::ESCAPED_SIMPLE_MARK_REGEXP,self::SIMPLE_PLACEHOLDER,$sqlStatement);
		$dynamics = $simpleDynamic->parse($sqlStatement);
		if(count($dynamics['parameters']) > 0)
		{
			$sql = new TSimpleDynamicSql($dynamics['parameters']);
			$sqlStatement = $dynamics['sql'];
		}
		else
			$sql = new TStaticSql();
		$sqlStatement=preg_replace('/'.self::SIMPLE_PLACEHOLDER.'/',self::SIMPLE_MARK,$sqlStatement);
		$sql->buildPreparedStatement($statement, $sqlStatement);
		$statement->setSqlText($sql);
	}

	/**
	 * Load select statement from xml mapping.
	 * @param SimpleXmlElement select node.
	 */
	protected function loadSelectTag($node)
	{
		$select = new TSqlMapSelect;
		$this->setObjectPropFromNode($select,$node);
		$this->processSqlStatement($select,$node);
		$mappedStatement = new TMappedStatement($this->_manager, $select);
		if(strlen($select->getCacheModel()) > 0)
			$mappedStatement = new TCachingStatement($mappedStatement);

		$this->_manager->addMappedStatement($mappedStatement);
	}

	/**
	 * Load insert statement from xml mapping.
	 * @param SimpleXmlElement insert node.
	 */
	protected function loadInsertTag($node)
	{
		$insert = $this->createInsertStatement($node);
		$this->processSqlStatement($insert, $node);
		$mappedStatement = new TInsertMappedStatement($this->_manager, $insert);
		$this->_manager->addMappedStatement($mappedStatement);
	}

	/**
	 * Create new insert statement from xml node.
	 * @param SimpleXmlElement insert node.
	 * @return TSqlMapInsert insert statement.
	 */
	protected function createInsertStatement($node)
	{
		$insert = new TSqlMapInsert;
		$this->setObjectPropFromNode($insert,$node);
		if(isset($node->selectKey))
			$this->loadSelectKeyTag($insert,$node->selectKey);
		return $insert;
	}

	/**
	 * Load the selectKey statement from xml mapping.
	 * @param SimpleXmlElement selectkey node
	 */
	protected function loadSelectKeyTag($insert, $node)
	{
		$selectKey = new TSqlMapSelectKey;
		$this->setObjectPropFromNode($selectKey,$node);
		$selectKey->setID($insert->getID());
		$selectKey->setID($insert->getID().'.SelectKey');
		$this->processSqlStatement($selectKey,$node);
		$insert->setSelectKey($selectKey);
		$mappedStatement = new TMappedStatement($this->_manager, $selectKey);
		$this->_manager->addMappedStatement($mappedStatement);
	}

	/**
	 * Load update statement from xml mapping.
	 * @param SimpleXmlElement update node.
	 */
	protected function loadUpdateTag($node)
	{
		$update = new TSqlMapUpdate;
		$this->setObjectPropFromNode($update,$node);
		$this->processSqlStatement($update, $node);
		$mappedStatement = new TUpdateMappedStatement($this->_manager, $update);
		$this->_manager->addMappedStatement($mappedStatement);
	}

	/**
	 * Load delete statement from xml mapping.
	 * @param SimpleXmlElement delete node.
	 */
	protected function loadDeleteTag($node)
	{
		$delete = new TSqlMapDelete;
		$this->setObjectPropFromNode($delete,$node);
		$this->processSqlStatement($delete, $node);
		$mappedStatement = new TDeleteMappedStatement($this->_manager, $delete);
		$this->_manager->addMappedStatement($mappedStatement);
	}

	/**
	 * Load procedure statement from xml mapping.
	 * @todo Implement loading procedure
	 * @param SimpleXmlElement procedure node
	 */
	protected function loadProcedureTag($node)
	{
		//var_dump('todo: add load procedure');
	}

	/**
	 * Load cache models from xml mapping.
	 * @param SimpleXmlElement cache node.
	 */
	protected function loadCacheModel($node)
	{
		$cacheModel = new TSqlMapCacheModel;
		$properties = array('id','implementation');
		foreach($node->attributes() as $name=>$value)
		{
			if(in_array(strtolower($name), $properties))
				$cacheModel->{'set'.$name}((string)$value);
		}
		$cache = Prado::createComponent($cacheModel->getImplementationClass(), $cacheModel);
		$this->setObjectPropFromNode($cache,$node,$properties);

		foreach($node->xpath('property') as $propertyNode)
		{
			$name = $propertyNode->attributes()->name;
			if($name===null || $name==='') continue;

			$value = $propertyNode->attributes()->value;
			if($value===null || $value==='') continue;

			if( !TPropertyAccess::has($cache, $name) ) continue;

			TPropertyAccess::set($cache, $name, $value);
		}

		$this->loadFlushInterval($cacheModel,$node);

		$cacheModel->initialize($cache);
		$this->_manager->addCacheModel($cacheModel);
		foreach($node->xpath('flushOnExecute') as $flush)
			$this->loadFlushOnCache($cacheModel,$node,$flush);
	}

	/**
	 * Load the flush interval
	 * @param TSqlMapCacheModel cache model
	 * @param SimpleXmlElement cache node
	 */
	protected function loadFlushInterval($cacheModel, $node)
	{
		$flushInterval = $node->xpath('flushInterval');
		if($flushInterval === null || count($flushInterval) === 0) return;
		$duration = 0;
		foreach($flushInterval[0]->attributes() as $name=>$value)
		{
			switch(strToLower($name))
			{
				case 'seconds':
					$duration += (integer)$value;
				break;
				case 'minutes':
					$duration += 60 * (integer)$value;
				break;
				case 'hours':
					$duration += 3600 * (integer)$value;
				break;
				case 'days':
					$duration += 86400 * (integer)$value;
				break;
				case 'duration':
					$duration = (integer)$value;
				break 2; // switch, foreach
			}
		}
		$cacheModel->setFlushInterval($duration);
	}

	/**
	 * Load the flush on cache properties.
	 * @param TSqlMapCacheModel cache model
	 * @param SimpleXmlElement parent node.
	 * @param SimpleXmlElement flush node.
	 */
	protected function loadFlushOnCache($cacheModel,$parent,$node)
	{
		$id = $cacheModel->getID();
		if(!isset($this->_FlushOnExecuteStatements[$id]))
			$this->_FlushOnExecuteStatements[$id] = array();
		foreach($node->attributes() as $name=>$value)
		{
			if(strtolower($name)==='statement')
				$this->_FlushOnExecuteStatements[$id][] = (string)$value;
		}
	}

	/**
	 * Attach CacheModel to statement and register trigger statements for cache models
	 */
	protected function registerCacheTriggers()
	{
		foreach($this->_FlushOnExecuteStatements as $cacheID => $statementIDs)
		{
			$cacheModel = $this->_manager->getCacheModel($cacheID);
			foreach($statementIDs as $statementID)
			{
				$statement = $this->_manager->getMappedStatement($statementID);
				$cacheModel->registerTriggerStatement($statement);
			}
		}
	}
}

