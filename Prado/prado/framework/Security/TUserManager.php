<?php
/**
 * TUserManager class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TUserManager.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Security
 */

/**
 * Using TUser class
 */
Prado::using('System.Security.TUser');

/**
 * TUserManager class
 *
 * TUserManager manages a static list of users {@link TUser}.
 * The user information is specified via module configuration using the following XML syntax,
 * <code>
 * <module id="users" class="System.Security.TUserManager" PasswordMode="Clear">
 *   <user name="Joe" password="demo" />
 *   <user name="John" password="demo" />
 *   <role name="Administrator" users="John" />
 *   <role name="Writer" users="Joe,John" />
 * </module>
 * </code>
 *
 * In addition, user information can also be loaded from an external file
 * specified by {@link setUserFile UserFile} property. Note, the property
 * only accepts a file path in namespace format. The user file format is
 * similar to the above sample.
 *
 * The user passwords may be specified as clear text, SH1 or MD5 hashed by setting
 * {@link setPasswordMode PasswordMode} as <b>Clear</b>, <b>SHA1</b> or <b>MD5</b>.
 * The default name for a guest user is <b>Guest</b>. It may be changed
 * by setting {@link setGuestName GuestName} property.
 *
 * TUserManager may be used together with {@link TAuthManager} which manages
 * how users are authenticated and authorized in a Prado application.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TUserManager.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Security
 * @since 3.0
 */
class TUserManager extends TModule implements IUserManager
{
	/**
	 * extension name to the user file
	 */
	const USER_FILE_EXT='.xml';

	/**
	 * @var array list of users managed by this module
	 */
	private $_users=array();
	/**
	 * @var array list of roles managed by this module
	 */
	private $_roles=array();
	/**
	 * @var string guest name
	 */
	private $_guestName='Guest';
	/**
	 * @var TUserManagerPasswordMode password mode
	 */
	private $_passwordMode=TUserManagerPasswordMode::MD5;
	/**
	 * @var boolean whether the module has been initialized
	 */
	private $_initialized=false;
	/**
	 * @var string user/role information file
	 */
	private $_userFile=null;

	/**
	 * Initializes the module.
	 * This method is required by IModule and is invoked by application.
	 * It loads user/role information from the module configuration.
	 * @param TXmlElement module configuration
	 */
	public function init($config)
	{
		$this->loadUserData($config);
		if($this->_userFile!==null)
		{
			$dom=new TXmlDocument;
			$dom->loadFromFile($this->_userFile);
			$this->loadUserData($dom);
		}
		$this->_initialized=true;
	}

	/**
	 * Loads user/role information from an XML node.
	 * @param TXmlElement the XML node containing the user information
	 */
	protected function loadUserData($xmlNode)
	{
		foreach($xmlNode->getElementsByTagName('user') as $node)
		{
			$name=trim(strtolower($node->getAttribute('name')));
			$this->_users[$name]=$node->getAttribute('password');
			if(($roles=trim($node->getAttribute('roles')))!=='')
			{
				foreach(explode(',',$roles) as $role)
				{
					if(($role=trim($role))!=='')
						$this->_roles[$name][]=$role;
				}
			}
		}
		foreach($xmlNode->getElementsByTagName('role') as $node)
		{
			foreach(explode(',',$node->getAttribute('users')) as $user)
			{
				if(($user=trim($user))!=='')
					$this->_roles[strtolower($user)][]=$node->getAttribute('name');
			}
		}
	}

	/**
	 * Returns an array of all users.
	 * Each array element represents a single user.
	 * The array key is the username in lower case, and the array value is the
	 * corresponding user password.
	 * @return array list of users
	 */
	public function getUsers()
	{
		return $this->_users;
	}

	/**
	 * Returns an array of user role information.
	 * Each array element represents the roles for a single user.
	 * The array key is the username in lower case, and the array value is
	 * the roles (represented as an array) that the user is in.
	 * @return array list of user role information
	 */
	public function getRoles()
	{
		return $this->_roles;
	}

	/**
	 * @return string the full path to the file storing user/role information
	 */
	public function getUserFile()
	{
		return $this->_userFile;
	}

	/**
	 * @param string user/role data file path (in namespace form). The file format is XML
	 * whose content is similar to that user/role block in application configuration.
	 * @throws TInvalidOperationException if the module is already initialized
	 * @throws TConfigurationException if the file is not in proper namespace format
	 */
	public function setUserFile($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('usermanager_userfile_unchangeable');
		else if(($this->_userFile=Prado::getPathOfNamespace($value,self::USER_FILE_EXT))===null || !is_file($this->_userFile))
			throw new TConfigurationException('usermanager_userfile_invalid',$value);
	}

	/**
	 * @return string guest name, defaults to 'Guest'
	 */
	public function getGuestName()
	{
		return $this->_guestName;
	}

	/**
	 * @param string name to be used for guest users.
	 */
	public function setGuestName($value)
	{
		$this->_guestName=$value;
	}

	/**
	 * @return TUserManagerPasswordMode how password is stored, clear text, or MD5 or SHA1 hashed. Default to TUserManagerPasswordMode::MD5.
	 */
	public function getPasswordMode()
	{
		return $this->_passwordMode;
	}

	/**
	 * @param TUserManagerPasswordMode how password is stored, clear text, or MD5 or SHA1 hashed.
	 */
	public function setPasswordMode($value)
	{
		$this->_passwordMode=TPropertyValue::ensureEnum($value,'TUserManagerPasswordMode');
	}

	/**
	 * Validates if the username and password are correct.
	 * @param string user name
	 * @param string password
	 * @return boolean true if validation is successful, false otherwise.
	 */
	public function validateUser($username,$password)
	{
		if($this->_passwordMode===TUserManagerPasswordMode::MD5)
			$password=md5($password);
		else if($this->_passwordMode===TUserManagerPasswordMode::SHA1)
			$password=sha1($password);
		$username=strtolower($username);
		return (isset($this->_users[$username]) && $this->_users[$username]===$password);
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
			$username=strtolower($username);
			if(isset($this->_users[$username]))
			{
				$user=new TUser($this);
				$user->setName($username);
				$user->setIsGuest(false);
				if(isset($this->_roles[$username]))
					$user->setRoles($this->_roles[$username]);
				return $user;
			}
			else
				return null;
		}
	}

	/**
	 * Returns a user instance according to auth data stored in a cookie.
	 * @param THttpCookie the cookie storing user authentication information
	 * @return TUser the user instance generated based on the cookie auth data, null if the cookie does not have valid auth data.
	 * @since 3.1.1
	 */
	public function getUserFromCookie($cookie)
	{
		if(($data=$cookie->getValue())!=='')
		{
			$data=unserialize($data);
			if(is_array($data) && count($data)===2)
			{
				list($username,$token)=$data;
				if(isset($this->_users[$username]) && $token===md5($username.$this->_users[$username]))
					return $this->getUser($username);
			}
		}
		return null;
	}

	/**
	 * Saves user auth data into a cookie.
	 * @param THttpCookie the cookie to receive the user auth data.
	 * @since 3.1.1
	 */
	public function saveUserToCookie($cookie)
	{
		$user=$this->getApplication()->getUser();
		$username=strtolower($user->getName());
		if(isset($this->_users[$username]))
		{
			$data=array($username,md5($username.$this->_users[$username]));
			$cookie->setValue(serialize($data));
		}
	}

	/**
	 * Sets a user as a guest.
	 * User name is changed as guest name, and roles are emptied.
	 * @param TUser the user to be changed to a guest.
	 */
	public function switchToGuest($user)
	{
		$user->setIsGuest(true);
	}
}

/**
 * TUserManagerPasswordMode class.
 * TUserManagerPasswordMode defines the enumerable type for the possible modes
 * that user passwords can be specified for a {@link TUserManager}.
 *
 * The following enumerable values are defined:
 * - Clear: the password is in plain text
 * - MD5: the password is recorded as the MD5 hash value of the original password
 * - SHA1: the password is recorded as the SHA1 hash value of the original password
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TUserManager.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Security
 * @since 3.0.4
 */
class TUserManagerPasswordMode extends TEnumerable
{
	const Clear='Clear';
	const MD5='MD5';
	const SHA1='SHA1';
}

