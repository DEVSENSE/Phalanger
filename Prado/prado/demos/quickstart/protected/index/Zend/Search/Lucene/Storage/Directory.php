<?php
/**
 * Zend Framework
 *
 * LICENSE
 *
 * This source file is subject to version 1.0 of the Zend Framework
 * license, that is bundled with this package in the file LICENSE, and
 * is available through the world-wide-web at the following URL:
 * http://www.zend.com/license/framework/1_0.txt. If you did not receive
 * a copy of the Zend Framework license and are unable to obtain it
 * through the world-wide-web, please send a note to license@zend.com
 * so we can mail you a copy immediately.
 *
 * @package    Zend_Search_Lucene
 * @subpackage Storage
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */


/**
 * @package    Zend_Search_Lucene
 * @subpackage Storage
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */
abstract class Zend_Search_Lucene_Storage_Directory
{

    /**
     * Closes the store.
     *
     * @return void
     */
    abstract public function close();

    /**
     * Returns an array of strings, one for each file in the directory.
     *
     * @return array
     */
    abstract public function fileList();

    /**
     * Creates a new, empty file in the directory with the given $filename.
     *
     * @param string $filename
     * @return Zend_Search_Lucene_Storage_File
     */
    abstract public function createFile($filename);


    /**
     * Removes an existing $filename in the directory.
     *
     * @param string $filename
     * @return void
     */
    abstract public function deleteFile($filename);


    /**
     * Returns true if a file with the given $filename exists.
     *
     * @param string $filename
     * @return boolean
     */
    abstract public function fileExists($filename);


    /**
     * Returns the length of a $filename in the directory.
     *
     * @param string $filename
     * @return integer
     */
    abstract public function fileLength($filename);


    /**
     * Returns the UNIX timestamp $filename was last modified.
     *
     * @param string $filename
     * @return integer
     */
    abstract public function fileModified($filename);


    /**
     * Renames an existing file in the directory.
     *
     * @param string $from
     * @param string $to
     * @return void
     */
    abstract public function renameFile($from, $to);


    /**
     * Sets the modified time of $filename to now.
     *
     * @param string $filename
     * @return void
     */
    abstract public function touchFile($filename);


    /**
     * Returns a Zend_Search_Lucene_Storage_File object for a given $filename in the directory.
     *
     * @param string $filename
     * @return Zend_Search_Lucene_Storage_File
     */
    abstract public function getFileObject($filename);

}

