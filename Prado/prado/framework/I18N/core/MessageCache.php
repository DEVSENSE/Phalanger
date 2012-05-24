<?php
/**
 * Translation table cache.
 * @author $Author: weizhuo $
 * @version $Id: MessageCache.php 2488 2008-08-06 01:34:06Z knut $
 * @package System.I18N.core
 */

/**
 * Load the cache lite library.
 */
require_once(dirname(__FILE__).'/TCache_Lite.php');

/**
 * Cache the translation table into the file system.
 * It can cache each cataloug+variant or just the whole section.
 * @package System.I18N.core
 * @author $Author: weizhuo $
 * @version $Id: MessageCache.php 2488 2008-08-06 01:34:06Z knut $
 */
class MessageCache 
{

	/**
	 * Cache Lite instance.
	 * @var TCache_Lite
	 */
	protected $cache;

	/**
	 * Caceh life time, default is 1 year.
	 */
	protected $lifetime = 3153600;
	

	/**
	 * Create a new Translation cache.
	 * @param string $cacheDir Directory to store the cache files.
	 */
	public function __construct($cacheDir)
	{		
		$cacheDir = $cacheDir.'/';
		
		if(!is_dir($cacheDir))
			throw new Exception(
				'The cache directory '.$cacheDir.' does not exists.'.
				'The cache directory must be writable by the server.');
		if(!is_writable($cacheDir))
			throw new Exception(
				'The cache directory '.$cacheDir.' must be writable '.
				'by the server.');
		
		$options = array(
			'cacheDir' => $cacheDir,
			'lifeTime' => $this->getLifeTime(),
			'automaticSerialization' => true
		);

		$this->cache = new TCache_Lite($options);
	}

	/**
	 * Get the cache life time.
	 * @return int Cache life time.
	 */
	public function getLifeTime()
	{
		return $this->lifetime;
	}

	/**
	 * Set the cache life time.
	 * @param int $time Cache life time.
	 */
	public function setLifeTime($time)
	{
		$this->lifetime = (int)$time;
	}

	/**
	 * Get the cache file ID based section and locale.
	 * @param string $catalogue The translation section.
	 * @param string $culture The translation locale, e.g. "en_AU".
	 */
	protected function getID($catalogue, $culture)
	{
		return $catalogue.':'.$culture;
	}

	/**
	 * Get the cache file GROUP based section and locale.
	 * @param string $catalogue The translation section.
	 * @param string $culture The translation locale, e.g. "en_AU".
	 */
	protected function getGroup($catalogue, $culture)
	{
		return $catalogue.':'.get_class($this);
	}

	/**
	 * Get the data from the cache.
	 * @param string $catalogue The translation section.
	 * @param string $culture The translation locale, e.g. "en_AU".
	 * @param string $filename If the source is a file, this file's modified
	 * time is newer than the cache's modified time, no cache hit. 
	 * @return mixed Boolean FALSE if no cache hit. Otherwise, translation
	 * table data for the specified section and locale.
	 */
	public function get($catalogue, $culture, $lastmodified=0) 
	{
		$ID = $this->getID($catalogue, $culture);
		$group = $this->getGroup($catalogue, $culture); 

		$this->cache->_setFileName($ID, $group);

		$cache = $this->cache->getCacheFile();
		
		if(is_file($cache) == false) 
			return false;


		$lastmodified = (int)$lastmodified;
		
		if($lastmodified <= 0 || $lastmodified > filemtime($cache))
			return false;		
		
		//echo '@@ Cache hit: "'.$ID.'" : "'.$group.'"';
		//echo "<br>\n";
			
		return $this->cache->get($ID, $group);
	}

	/**
	 * Save the data to cache for the specified section and locale.
	 * @param array $data The data to save.
	 * @param string $catalogue The translation section.
	 * @param string $culture The translation locale, e.g. "en_AU".
	 */
	public function save($data, $catalogue, $culture) 
	{		
		$ID = $this->getID($catalogue, $culture);
		$group = $this->getGroup($catalogue, $culture); 
		
		//echo '## Cache save: "'.$ID.'" : "'.$group.'"';
		//echo "<br>\n";
		
		return $this->cache->save($data, $ID, $group);
	}
	
	/**
	 * Clean up the cache for the specified section and locale.
	 * @param string $catalogue The translation section.
	 * @param string $culture The translation locale, e.g. "en_AU".
	 */
	public function clean($catalogue, $culture) 
	{
		$group = $this->getGroup($catalogue, $culture); 
		$this->cache->clean($group);
	}
	
	/**
	 * Flush the cache. Deletes all the cache files.
	 */
	public function clear()
	{
		$this->cache->clean();
	}

}

?>
