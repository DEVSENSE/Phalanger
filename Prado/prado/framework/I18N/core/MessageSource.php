<?php

/**
 * MessageSource class file.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the BSD License.
 *
 * Copyright(c) 2004 by Qiang Xue. All rights reserved.
 *
 * To contact the author write to {@link mailto:qiang.xue@gmail.com Qiang Xue}
 * The latest version of PRADO can be obtained from:
 * {@link http://prado.sourceforge.net/}
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Revision: 1.4 $  $Date: 2005/12/17 06:11:28 $
 * @package System.I18N.core
 */

 /**
  * Get the IMessageSource interface.
  */
require_once(dirname(__FILE__).'/IMessageSource.php');

/**
 * Get the MessageCache class file.
 */
require_once(dirname(__FILE__).'/MessageCache.php');

/**
 * Abstract MessageSource class.
 * 
 * The base class for all MessageSources. Message sources must be instantiated
 * using the factory method. The default valid sources are
 *
 *  # XLIFF -- using XML XLIFF format to store the translation messages.
 *  # gettext -- Translated messages are stored in the gettext format.
 *  # Database -- Use an existing TDbConnection to store the messages.
 *  # SQLite -- (Deprecated) Store the translation messages in a SQLite database.
 *  # MySQL -- (Deprecated) Using a MySQL database to store the messages.
 *
 * A custom message source can be instantiated by specifying the filename
 * parameter to point to the custom class file. E.g.
 * <code>
 *   $resource = '...'; //custom message source resource
 *   $classfile = '../MessageSource_MySource.php'; //custom message source
 *   $source = MessageSource::factory('MySource', $resource, $classfile);
 * </code>
 * 
 * If you are writting your own message sources, pay attention to the 
 * loadCatalogue method. It details how the resources are loaded and cached.
 * See also the existing message source types as examples.
 * 
 * The following example instantiates a Database message source, set the culture,
 * set the cache handler, and use the source in a message formatter. 
 * The messages are stored using an existing connection. The source parameter
 * for the factory method must contain a valid ConnectionID.
 * <code>
 *   // db1 must be already configured
 *   $source = MessageSource::factory('Database', 'db1');
 *   
 *   //set the culture and cache, store the cache in the /tmp directory.
 *   $source->setCulture('en_AU')l
 *   $source->setCache(new MessageCache('/tmp'));
 *   
 *   $formatter = new MessageFormat($source);
 * </code>
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version v1.0, last update on Fri Dec 24 19:55:49 EST 2004
 * @package System.I18N.core
 */
abstract class MessageSource implements IMessageSource
{	
	/**
	 * The culture name for this message source.
	 * @var string 
	 */
	protected $culture;
	
	/**
	 * Array of translation messages.
	 * @var array 
	 */
	protected $messages = array();

	/**
	 * The source of message translations.
	 * @var string 
	 */
	protected $source;
	
	/**
	 * The translation cache.
	 * @var MessageCache 
	 */
	protected $cache;
	
	protected $untranslated = array();

	/**
	 * Private constructor. MessageSource must be initialized using
	 * the factory method.
	 */
	private function __construct()
	{	
		//throw new Exception('Please use the factory method to instantiate.');
	}
	
	/**
	 * Factory method to instantiate a new MessageSource depending on the
	 * source type. The allowed source types are 'XLIFF', 'gettext' and
     * 'Database'. The source parameter depends on the source type. 
     * For 'gettext' and 'XLIFF', 'source' should point to the directory 
     * where the messages are stored. 
     * For 'Database', 'source' must be a valid connection id.
     * If one of the deprecated types 'MySQL' or 'SQLite' is used, 
     * 'source' must contain a valid  DSN.
	 *
 	 * Custom message source are possible by supplying the a filename parameter
 	 * in the factory method.
	 * 
	 * @param string the message source type.
	 * @param string the location of the resource or the ConnectionID.
	 * @param string the filename of the custom message source.
	 * @return MessageSource a new message source of the specified type. 
	 * @throws InvalidMessageSourceTypeException
	 */
	static function &factory($type, $source='.', $filename='')
	{
		$types = array('XLIFF','gettext','Database','MySQL','SQLite');
		
		if(empty($filename) && !in_array($type, $types))
			throw new Exception('Invalid type "'.$type.'", valid types are '.
				implode(', ', $types));
		
		$class = 'MessageSource_'.$type;
		
		if(empty($filename))
			$filename = dirname(__FILE__).'/'.$class.'.php';
						
		if(is_file($filename) == false)
			throw new Exception("File $filename not found");
						
		include_once $filename;
        
		$obj =  new $class($source);
		
		return $obj;
	}
	
	/**
	 * Load a particular message catalogue. Use read() to 
	 * to get the array of messages. The catalogue loading sequence
	 * is as follows
	 *
	 *  # [1] call getCatalogeList($catalogue) to get a list of 
	 *    variants for for the specified $catalogue.
	 *  # [2] for each of the variants, call getSource($variant)
	 *    to get the resource, could be a file or catalogue ID.
	 *  # [3] verify that this resource is valid by calling isValidSource($source)
	 *  # [4] try to get the messages from the cache
	 *  # [5] if a cache miss, call load($source) to load the message array
	 *  # [6] store the messages to cache.
	 *  # [7] continue with the foreach loop, e.g. goto [2].
	 * 
	 * @param string a catalogue to load
	 * @return boolean true if loaded, false otherwise.	 
	 * @see read()
	 */
	function load($catalogue='messages')
	{
		$variants = $this->getCatalogueList($catalogue);
		
		$this->messages = array();
		
		foreach($variants as $variant)
		{
			$source = $this->getSource($variant);
			
			if($this->isValidSource($source) == false) continue;

			$loadData = true;
			
			if($this->cache)
			{
				$data = $this->cache->get($variant, 
					$this->culture, $this->getLastModified($source));
				
				if(is_array($data))
				{
					$this->messages[$variant] = $data;
					$loadData = false;
				}
				unset($data);
			}
			if($loadData)
			{
				$data = &$this->loadData($source);
				if(is_array($data))
				{
					$this->messages[$variant] = $data;
					if($this->cache)
						$this->cache->save($data, $variant, $this->culture);
				}	
				unset($data);
			}
		}
		
		return true;
	}	
	
	/**
	 * Get the array of messages.
	 * @param parameter
	 * @return array translation messages. 
	 */
	public function read()
	{
		return $this->messages;
	}
		
	/**
	 * Get the cache handler for this source.
	 * @return MessageCache cache handler
	 */
	public function getCache()
	{
		return $this->cache;
	}
	
	/**
	 * Set the cache handler for caching the messages.
	 * @param MessageCache the cache handler.
	 */
	public function setCache(MessageCache $cache)
	{
		$this->cache = $cache;
	}
	
	/**
	 * Add a untranslated message to the source. Need to call save()
	 * to save the messages to source.
	 * @param string message to add
	 */	
	public function append($message)
	{
		if(!in_array($message, $this->untranslated))
			$this->untranslated[] = $message;
	}
	
	/**
	 * Set the culture for this message source.
	 * @param string culture name
	 */
	public function setCulture($culture)
	{
		$this->culture = $culture;
	}	
	
	/**
	 * Get the culture identifier for the source.
	 * @return string culture identifier. 
	 */
	public function getCulture()
	{
		return $this->culture;
	}	

	/**
	 * Get the last modified unix-time for this particular catalogue+variant.
	 * @param string catalogue+variant
	 * @return int last modified in unix-time format.
	 */
	protected function getLastModified($source)
	{
		return 0;
	}
	
	/**
	 * Load the message for a particular catalogue+variant.
	 * This methods needs to implemented by subclasses.
	 * @param string catalogue+variant.
	 * @return array of translation messages. 
	 */
	protected function &loadData($variant)
	{
		return array();
	}
	
	/**
	 * Get the source, this could be a filename or database ID.
	 * @param string catalogue+variant
	 * @return string the resource key 
	 */
	protected function getSource($variant)
	{
		return $variant;
	}
	
	/**
	 * Determine if the source is valid.
	 * @param string catalogue+variant
	 * @return boolean true if valid, false otherwise. 
	 */
	protected function isValidSource($source)
	{
		return false;
	}
	
	/**
	 * Get all the variants of a particular catalogue.
	 * This method must be implemented by subclasses.
	 * @param string catalogue name
	 * @return array list of all variants for this catalogue. 
	 */	
	protected function getCatalogueList($catalogue)
	{
		return array();
	}		
}


/**
 * TMessageSourceIOException thrown when unable to modify message source
 * data.
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @version $Revision: 1.4 $  $Date: 2005/12/17 06:11:28 ${DATE} ${TIME} $
 * @package System.I18N.core
 */
class TMessageSourceIOException extends TException
{

}
?>
