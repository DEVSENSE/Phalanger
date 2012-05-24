<?php

/**
 * TCache_Lite class file.
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
 * @version $Revision: 1.3 $  $Date: 2005/10/09 10:24:12 $
 * @package System.I18N.core
 */

/**
* Fast, light and safe Cache Class
*
* TCache_Lite is a fast, light and safe cache system. It's optimized
* for file containers. It is fast and safe (because it uses file
* locking and/or anti-corruption tests).
*
* There are some examples in the 'docs/examples' file
* Technical choices are described in the 'docs/technical' file
*
* A tutorial is available in english at this url :
* http://www.pearfr.org/index.php/en/article/cache_lite
* (big thanks to Pierre-Alain Joye for the translation)
*
* The same tutorial is also available in french at this url :
* http://www.pearfr.org/index.php/fr/article/cache_lite
*
* Memory Caching is from an original idea of
* Mike BENOIT <ipso@snappymail.ca>
*
* @package System.I18N.core
* @version $Id: TCache_Lite.php 2838 2010-05-07 08:34:40Z rojaro $
* @author Fabien MARTY <fab@php.net>
* @copyright  1997-2005 The PHP Group
* @license    http://www.gnu.org/copyleft/lesser.html GNU LGPL
* @link       http://pear.php.net/package/Cache_Lite
*/
class TCache_Lite
{

    // --- Private properties ---

    /**
    * Directory where to put the cache files
    * (make sure to add a trailing slash)
    *
    * @var string $_cacheDir
    */
    protected $_cacheDir = '/tmp/';

    /**
    * Enable / disable caching
    *
    * (can be very usefull for the debug of cached scripts)
    *
    * @var boolean $_caching
    */
    protected $_caching = true;

    /**
    * Cache lifetime (in seconds)
    *
    * @var int $_lifeTime
    */
    protected $_lifeTime = 3600;

    /**
    * Enable / disable fileLocking
    *
    * (can avoid cache corruption under bad circumstances)
    *
    * @var boolean $_fileLocking
    */
    protected $_fileLocking = true;

    /**
    * Timestamp of the last valid cache
    *
    * @var int $_refreshTime
    */
    protected $_refreshTime;

    /**
    * File name (with path)
    *
    * @var string $_file
    */
    protected $_file;

    /**
    * Enable / disable write control (the cache is read just after writing
    * to detect corrupt entries)
    *
    * Enable write control will lightly slow the cache writing but not the
    * cache reading. Write control can detect some corrupt cache files but
    * maybe it's not a perfect control
    *
    * @var boolean $_writeControl
    */
    protected $_writeControl = true;

    /**
    * Enable / disable read control
    *
    * If enabled, a control key is embeded in cache file and this key is
    * compared with the one calculated after the reading.
    *
    * @var boolean $_writeControl
    */
    protected $_readControl = true;

    /**
    * Type of read control (only if read control is enabled)
    *
    * Available values are :
    * 'md5' for a md5 hash control (best but slowest)
    * 'crc32' for a crc32 hash control (lightly less safe but faster,
    * better choice)
    * 'strlen' for a length only test (fastest)
    *
    * @var boolean $_readControlType
    */
    protected $_readControlType = 'crc32';

    /**
    * Current cache id
    *
    * @var string $_id
    */
    protected $_id;

    /**
    * Current cache group
    *
    * @var string $_group
    */
    protected $_group;

    /**
    * Enable / Disable "Memory Caching"
    *
    * NB : There is no lifetime for memory caching !
    *
    * @var boolean $_memoryCaching
    */
    protected $_memoryCaching = false;

    /**
    * Enable / Disable "Only Memory Caching"
    * (be carefull, memory caching is "beta quality")
    *
    * @var boolean $_onlyMemoryCaching
    */
    protected $_onlyMemoryCaching = false;

    /**
    * Memory caching array
    *
    * @var array $_memoryCachingArray
    */
    protected $_memoryCachingArray = array();

    /**
    * Memory caching counter
    *
    * @var int $memoryCachingCounter
    */
    protected $_memoryCachingCounter = 0;

    /**
    * Memory caching limit
    *
    * @var int $memoryCachingLimit
    */
    protected $_memoryCachingLimit = 1000;

    /**
    * File Name protection
    *
    * if set to true, you can use any cache id or group name
    * if set to false, it can be faster but cache ids and group names
    * will be used directly in cache file names so be carefull with
    * special characters...
    *
    * @var boolean $fileNameProtection
    */
    protected $_fileNameProtection = true;

    /**
    * Enable / disable automatic serialization
    *
    * it can be used to save directly datas which aren't strings
    * (but it's slower)
    *
    * @var boolean $_serialize
    */
    protected $_automaticSerialization = false;

    // --- Public methods ---

    /**
    * Constructor
    *
    * $options is an assoc. Available options are :
    * $options = array(
    * 'cacheDir' => directory where to put the cache files (string),
    * 'caching' => enable / disable caching (boolean),
    * 'lifeTime' => cache lifetime in seconds (int),
    * 'fileLocking' => enable / disable fileLocking (boolean),
    * 'writeControl' => enable / disable write control (boolean),
    * 'readControl' => enable / disable read control (boolean),
    * 'readControlType' => type of read control 'crc32', 'md5', 'strlen',
    * 'memoryCaching' => enable / disable memory caching (boolean),
    * 'onlyMemoryCaching' => enable / disable only memory caching (boolean),
    * 'memoryCachingLimit' => max nbr of records in memory caching (int),
    * 'fileNameProtection' => enable / disable file name protection (boolean),
    * 'automaticSerialization' => enable / disable serialization (boolean)
    * );
    *
    * @param array $options options
    * @access public
    */
    function TCache_Lite($options = array(null))
    {
        $availableOptions = array(	'automaticSerialization',
        							'fileNameProtection',
        							'memoryCaching',
        							'onlyMemoryCaching',
        							'memoryCachingLimit',
        							'cacheDir',
        							'caching',
        							'lifeTime',
        							'fileLocking',
        							'writeControl',
        							'readControl',
        							'readControlType');
        foreach($options as $key => $value) {
            if(in_array($key, $availableOptions)) {
                $property = '_'.$key;
                $this->$property = $value;
            }
        }
        $this->_refreshTime = time() - $this->_lifeTime;
	}

    /**
    * Test if a cache is available and (if yes) return it
    *
    * @param string $id cache id
    * @param string $group name of the cache group
    * @param boolean $doNotTestCacheValidity if set to true, the cache
    * validity won't be tested
    * @return string data of the cache (or false if no cache available)
    * @access public
    */
    function get($id, $group = 'default', $doNotTestCacheValidity = false)
    {
        $this->_id = $id;
        $this->_group = $group;
        $data = false;
        if ($this->_caching) {
            $this->_setFileName($id, $group);
            if ($this->_memoryCaching) {
                if (isset($this->_memoryCachingArray[$this->_file])) {
                    if ($this->_automaticSerialization) {
                        return unserialize(
                        			$this->_memoryCachingArray[$this->_file]);
                    } else {
                        return $this->_memoryCachingArray[$this->_file];
                    }
                } else {
                    if ($this->_onlyMemoryCaching) {
                        return false;
                    }
                }
            }
            if ($doNotTestCacheValidity) {
                if (file_exists($this->_file)) {
                    $data = $this->_read();
                }
            } else {
                if (@filemtime($this->_file) > $this->_refreshTime) {
                    $data = $this->_read();
                }
            }
            if (($data) and ($this->_memoryCaching)) {
                $this->_memoryCacheAdd($this->_file, $data);
            }
            if ($this->_automaticSerialization && is_string($data)) {
                $data = unserialize($data);
            }
            return $data;
        }
        return false;
    }

    /**
    * Save some data in a cache file
    *
    * @param string $data data to put in cache (can be another type than strings
    * if automaticSerialization is on)
    * @param string $id cache id
    * @param string $group name of the cache group
    * @return boolean true if no problem
    * @access public
    */
    function save($data, $id = null, $group = 'default')
    {
        if ($this->_caching) {
            if ($this->_automaticSerialization) {
                $data = serialize($data);
            }
            if (isset($id)) {
                $this->_setFileName($id, $group);
            }
            if ($this->_memoryCaching) {
                $this->_memoryCacheAdd($this->_file, $data);
                if ($this->_onlyMemoryCaching) {
                    return true;
                }
            }
            if ($this->_writeControl) {
                if (!$this->_writeAndControl($data)) {
                    @touch($this->_file, time() - 2*abs($this->_lifeTime));
                    return false;
                } else {
                    return true;
                }
            } else {
                return $this->_write($data);
            }
        }
        return false;
    }

    /**
    * Remove a cache file
    *
    * @param string $id cache id
    * @param string $group name of the cache group
    * @return boolean true if no problem
    * @access public
    */
    function remove($id, $group = 'default')
    {
        $this->_setFileName($id, $group);
        if (!@unlink($this->_file)) {
            $this->raiseError('TCache_Lite : Unable to remove cache !', -3);
            return false;
        }
        return true;
    }

    /**
    * Clean the cache
    *
    * if no group is specified all cache files will be destroyed
    * else only cache files of the specified group will be destroyed
    *
    * @param string $group name of the cache group
    * @return boolean true if no problem
    * @access public
    */
    function clean($group = false)
    {
        if ($this->_fileNameProtection) {
            $motif = ($group) ? 'cache_'.md5($group).'_' : 'cache_';
        } else {
            $motif = ($group) ? 'cache_'.$group.'_' : 'cache_';
        }
        if ($this->_memoryCaching) {
            while (list($key, $value) = each($this->_memoryCaching)) {
                if (strpos($key, $motif, 0)) {
                    unset($this->_memoryCaching[$key]);
                    $this->_memoryCachingCounter =
                    		$this->_memoryCachingCounter - 1;
                }
            }
            if ($this->_onlyMemoryCaching) {
                return true;
            }
        }
        if (!($dh = opendir($this->_cacheDir))) {
            $this->raiseError('TCache_Lite : Unable to open cache directory !');
            return false;
        }
        while ($file = readdir($dh)) {
            if (($file != '.') && ($file != '..')) {
                $file = $this->_cacheDir . $file;
                if (is_file($file)) {
                    if (strpos($file, $motif, 0)) {
                        if (!@unlink($file)) {
             $this->raiseError('Cache_Lite : Unable to remove cache !', -3);
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

	    /**
    * Set a new life time
    *
    * @param int $newLifeTime new life time (in seconds)
    * @access public
    */
    function setLifeTime($newLifeTime)
    {
        $this->_lifeTime = $newLifeTime;
        $this->_refreshTime = time() - $newLifeTime;
    }

    /**
    *
    * @access public
    */
    function saveMemoryCachingState($id, $group = 'default')
    {
        if ($this->_caching) {
            $array = array(
                'counter' => $this->_memoryCachingCounter,
                'array' => $this->_memoryCachingState
            );
            $data = serialize($array);
            $this->save($data, $id, $group);
        }
    }

    /**
    *
    * @access public
    */
    function getMemoryCachingState($id, $group = 'default',
    								$doNotTestCacheValidity = false)
    {
        if ($this->_caching) {
            if ($data = $this->get($id, $group, $doNotTestCacheValidity))
            {
                $array = unserialize($data);
                $this->_memoryCachingCounter = $array['counter'];
                $this->_memoryCachingArray = $array['array'];
            }
        }
    }

    /**
    * Return the cache last modification time
    *
    * BE CAREFUL : THIS METHOD IS FOR HACKING ONLY !
    *
    * @return int last modification time
    */
    function lastModified() {
        return filemtime($this->cache->_file);
    }

    /**
    * Trigger a PEAR error
    *
    * To improve performances, the PEAR.php file is included dynamically.
    * The file is so included only when an error is triggered. So, in most
    * cases, the file isn't included and perfs are much better.
    *
    * @param string $msg error message
    * @param int $code error code
    * @access public
    */
    function raiseError($msg, $code)
    {
       throw new Exception($msg);
    }

    // --- Private methods ---

    /**
    *
    * @access private
    */
    function _memoryCacheAdd($id, $data)
    {
        $this->_memoryCachingArray[$this->_file] = $data;
        if ($this->_memoryCachingCounter >= $this->_memoryCachingLimit) {
            list($key, $value) = each($this->_memoryCachingArray);
            unset($this->_memoryCachingArray[$key]);
        } else {
            $this->_memoryCachingCounter = $this->_memoryCachingCounter + 1;
        }
    }

    /**
    * Make a file name (with path)
    *
    * @param string $id cache id
    * @param string $group name of the group
    * @access private
    */
    function _setFileName($id, $group)
    {
        if ($this->_fileNameProtection) {
            $this->_file = ($this->_cacheDir.'cache_'.md5($group).'_'
            						.md5($id));
        } else {
            $this->_file = $this->_cacheDir.'cache_'.$group.'_'.$id;
        }
    }

    function getCacheFile()
    {
    	return $this->_file;
    }

    /**
    * Read the cache file and return the content
    *
    * @return string content of the cache file
    * @access private
    */
    function _read()
    {
        $fp = @fopen($this->_file, "rb");
        if ($this->_fileLocking) @flock($fp, LOCK_SH);
        if ($fp) {
        	// because the filesize can be cached by PHP itself...
            clearstatcache();
            $length = @filesize($this->_file);
			if(version_compare(PHP_VERSION, '5.3.0', 'lt'))
			{
	            $mqr = get_magic_quotes_runtime();
    	        set_magic_quotes_runtime(0);
			}
            if ($this->_readControl) {
                $hashControl = @fread($fp, 32);
                $length = $length - 32;
            }
            $data = @fread($fp, $length);
			if(isset($mqr))
	            set_magic_quotes_runtime($mqr);
            if ($this->_fileLocking) @flock($fp, LOCK_UN);
            @fclose($fp);
            if ($this->_readControl) {
                $hashData = $this->_hash($data, $this->_readControlType);
                if ($hashData != $hashControl) {
                    @touch($this->_file, time() - 2*abs($this->_lifeTime));
                    return false;
                }
            }
            return $data;
        }
        $this->raiseError('Cache_Lite : Unable to read cache !', -2);
        return false;
    }

    /**
    * Write the given data in the cache file
    *
    * @param string $data data to put in cache
    * @return boolean true if ok
    * @access private
    */
    function _write($data)
    {
        $fp = @fopen($this->_file, "wb");
        if ($fp) {
            if ($this->_fileLocking) @flock($fp, LOCK_EX);
            if ($this->_readControl) {
                @fwrite($fp, $this->_hash($data, $this->_readControlType), 32);
            }
            $len = strlen($data);
            @fwrite($fp, $data, $len);
            if ($this->_fileLocking) @flock($fp, LOCK_UN);
            @fclose($fp);
            return true;
        }
        $this->raiseError('Cache_Lite : Unable to write cache !', -1);
        return false;
    }

    /**
    * Write the given data in the cache file and control it just after to avoid
    * corrupted cache entries
    *
    * @param string $data data to put in cache
    * @return boolean true if the test is ok
    * @access private
    */
    function _writeAndControl($data)
    {
        $this->_write($data);
        $dataRead = $this->_read($data);
        return ($dataRead==$data);
    }

    /**
    * Make a control key with the string containing datas
    *
    * @param string $data data
    * @param string $controlType type of control 'md5', 'crc32' or 'strlen'
    * @return string control key
    * @access private
    */
    function _hash($data, $controlType)
    {
        switch ($controlType) {
        case 'md5':
            return md5($data);
        case 'crc32':
            return sprintf('% 32d', crc32($data));
        case 'strlen':
            return sprintf('% 32d', strlen($data));
        default:
            $this->raiseError('Unknown controlType ! '.
            '(available values are only \'md5\', \'crc32\', \'strlen\')', -5);
        }
    }

}

?>
