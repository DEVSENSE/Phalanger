<?php
/**
 * TSqlMapConfig class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSqlMapConfig.php 2745 2009-11-08 15:11:14Z godzilla80@gmx.net $
 * @package System.Data.SqlMap
 */

Prado::using('System.Data.TDataSourceConfig');

/**
 * TSqlMapConfig module configuration class.
 *
 * Database connection and TSqlMapManager configuration.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapConfig.php 2745 2009-11-08 15:11:14Z godzilla80@gmx.net $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TSqlMapConfig extends TDataSourceConfig
{
	private $_configFile;
	private $_sqlmap;
	private $_enableCache=false;

	/**
	 * File extension of external configuration file
	 */
	const CONFIG_FILE_EXT='.xml';

	/**
	 * @return string module ID + configuration file path.
	 */
	private function getCacheKey()
	{
		return $this->getID().$this->getConfigFile();
	}

	/**
	 * Deletes the configuration cache.
	 */
	public function clearCache()
	{
		$cache = $this->getApplication()->getCache();
		if($cache !== null) {
			$cache->delete($this->getCacheKey());
		}
	}

	/**
	 * Create and configure the data mapper using sqlmap configuration file.
	 * Or if cache is enabled and manager already cached load from cache.
	 * If cache is enabled, the data mapper instance is cached.
	 *
	 * @return TSqlMapManager SqlMap manager instance
	 * @since 3.1.7
	 */
	public function getSqlMapManager() {
		Prado::using('System.Data.SqlMap.TSqlMapManager');
		if(($manager = $this->loadCachedSqlMapManager())===null)
		{
			$manager = new TSqlMapManager($this->getDbConnection());
			if(strlen($file=$this->getConfigFile()) > 0)
			{
				$manager->configureXml($file);
				$this->cacheSqlMapManager($manager);
			}
		}
		elseif($this->getConnectionID() !== '') {
			$manager->setDbConnection($this->getDbConnection());
		}
		return $manager;
	}

	/**
	 * Saves the current SqlMap manager to cache.
	 * @return boolean true if SqlMap manager was cached, false otherwise.
	 */
	protected function cacheSqlMapManager($manager)
	{
		if($this->getEnableCache())
		{
			$cache = $this->getApplication()->getCache();
			if($cache !== null) {
				$dependencies = null;
				if($this->getApplication()->getMode() !== TApplicationMode::Performance)
					$dependencies = $manager->getCacheDependencies();
				return $cache->set($this->getCacheKey(), $manager, 0, $dependencies);
			}
		}
		return false;
	}

	/**
	 * Loads SqlMap manager from cache.
	 * @return TSqlMapManager SqlMap manager intance if load was successful, null otherwise.
	 */
	protected function loadCachedSqlMapManager()
	{
		if($this->getEnableCache())
		{
			$cache = $this->getApplication()->getCache();
			if($cache !== null)
			{
				$manager = $cache->get($this->getCacheKey());
				if($manager instanceof TSqlMapManager)
					return $manager;
			}
		}
		return null;
	}

	/**
	 * @return string SqlMap configuration file.
	 */
	public function getConfigFile()
	{
		return $this->_configFile;
	}

	/**
	 * @param string external configuration file in namespace format. The file
	 * extension must be '.xml'.
	 * @throws TConfigurationException if the file is invalid.
	 */
	public function setConfigFile($value)
	{
		if(is_file($value))
			$this->_configFile=$value;
		else
		{
			$file = Prado::getPathOfNamespace($value,self::CONFIG_FILE_EXT);
			if($file === null || !is_file($file))
				throw new TConfigurationException('sqlmap_configfile_invalid',$value);
			else
				$this->_configFile = $file;
		}
	}

	/**
	 * Set true to cache sqlmap instances.
	 * @param boolean true to cache sqlmap instance.
	 */
	public function setEnableCache($value)
	{
		$this->_enableCache = TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return boolean true if configuration should be cached, false otherwise.
	 */
	public function getEnableCache()
	{
		return $this->_enableCache;
	}

	/**
	 * @return TSqlMapGateway SqlMap gateway instance.
	 */
	protected function createSqlMapGateway()
	{
		return $this->getSqlMapManager()->getSqlmapGateway();
	}

	/**
	 * Initialize the sqlmap if necessary, returns the TSqlMapGateway instance.
	 * @return TSqlMapGateway SqlMap gateway instance.
	 */
	public function getClient()
	{
		if($this->_sqlmap===null )
			$this->_sqlmap=$this->createSqlMapGateway();
		return $this->_sqlmap;
	}
}

