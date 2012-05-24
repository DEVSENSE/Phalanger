<?php
/**
 * TimeTrackerUser class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TimeTrackerUser.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 */

/**
 * Import TUser and TUserManager
 */
Prado::using('System.Security.TUser');
Prado::using('System.Security.TUserManager');

/**
 * User class for Time Tracker application.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TimeTrackerUser.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 * @since 3.1
 */
class TimeTrackerUser extends TUser
{
	private $_emailAddress;
	
	/**
	 * @param string user email address
	 */
	public function setEmailAddress($value)
	{
		$this->_emailAddress = $value;
	}
	
	/**
	 * @return string user email address
	 */
	public function getEmailAddress()
	{
		return $this->_emailAddress;
	}
}

?>