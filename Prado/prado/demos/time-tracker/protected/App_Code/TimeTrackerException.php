<?php
/**
 * TimeTrackerException class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TimeTrackerException.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 */

/**
 * Generic time tracker application exception. Exception messages are saved in
 * "exceptions.txt"
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TimeTrackerException.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 * @since 3.1
 */
class TimeTrackerException extends TException
{
	/**
	 * @return string path to the error message file
	 */
	protected function getErrorMessageFile()
	{
		return dirname(__FILE__).'/exceptions.txt';
	}	
}

?>