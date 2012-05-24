<?php
/**
 * IUserManager interface file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: IUserManager.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Security
 */

/**
 * IUserManager interface
 *
 * IUserManager specifies the interface that must be implemented by
 * a user manager class if it is to be used together with {@link TAuthManager}
 * and {@link TUser}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: IUserManager.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Security
 * @since 3.0
 */
interface IUserManager
{
	/**
	 * @return string name for a guest user.
	 */
	public function getGuestName();
	/**
	 * Returns a user instance given the user name.
	 * @param string user name, null if it is a guest.
	 * @return TUser the user instance, null if the specified username is not in the user database.
	 */
	public function getUser($username=null);
	/**
	 * Returns a user instance according to auth data stored in a cookie.
	 * @param THttpCookie the cookie storing user authentication information
	 * @return TUser the user instance generated based on the cookie auth data, null if the cookie does not have valid auth data.
	 * @since 3.1.1
	 */
	public function getUserFromCookie($cookie);
	/**
	 * Saves user auth data into a cookie.
	 * @param THttpCookie the cookie to receive the user auth data.
	 * @since 3.1.1
	 */
	public function saveUserToCookie($cookie);
	/**
	 * Validates if the username and password are correct.
	 * @param string user name
	 * @param string password
	 * @return boolean true if validation is successful, false otherwise.
	 */
	public function validateUser($username,$password);
}

