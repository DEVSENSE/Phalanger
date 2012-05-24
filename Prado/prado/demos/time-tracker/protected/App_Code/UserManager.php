<?php
/**
 * UserManager class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: UserManager.php 2289 2007-10-01 01:04:10Z xue $
 * @package Demos
 */

/**
 * User manager module class for time tracker application.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: UserManager.php 2289 2007-10-01 01:04:10Z xue $
 * @package Demos
 * @since 3.1
 */
class UserManager extends TModule implements IUserManager
{
	/**
	 * @return string name for a guest user.
	 */
	public function getGuestName()
	{
		return 'Guest';
	}

	/**
	 * Returns a user instance given the user name.
	 * @param string user name, null if it is a guest.
	 * @return TUser the user instance, null if the specified username is not in the user database.
	 */
	public function getUser($username=null)
	{
		if($username===null)
		{
			$user=new TUser($this);
			$user->setIsGuest(true);
			return $user;
		}
		else
		{
			$daos = $this->getApplication()->getModule('daos');
			$userDao = $daos->getDao('UserDao');
			$user = $userDao->getUserByName($username);
			$user->setIsGuest(false);
			return $user;
		}
	}

	/**
	 * Validates if the username and password are correct.
	 * @param string user name
	 * @param string password
	 * @return boolean true if validation is successful, false otherwise.
	 */
	public function validateUser($username,$password)
	{
		$daos = $this->getApplication()->getModule('daos');
		$userDao = $daos->getDao('UserDao');
		return $userDao->validateUser($username, $password);
	}

	/**
	 * Saves user auth data into a cookie.
	 * @param THttpCookie the cookie to receive the user auth data.
	 * @since 3.1.1
	 */
	public function saveUserToCookie($cookie)
	{
		// do nothing since we don't support cookie-based auth in this example
	}

	/**
	 * Returns a user instance according to auth data stored in a cookie.
	 * @param THttpCookie the cookie storing user authentication information
	 * @return TUser the user instance generated based on the cookie auth data, null if the cookie does not have valid auth data.
	 * @since 3.1.1
	 */
	public function getUserFromCookie($cookie)
	{
		// do nothing since we don't support cookie-based auth in this example
		return null;
	}
}

?>