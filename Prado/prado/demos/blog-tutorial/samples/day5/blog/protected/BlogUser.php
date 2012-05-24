<?php

// Include TDbUserManager.php file which defines TDbUser
Prado::using('System.Security.TDbUserManager');

/**
 * BlogUser Class.
 * BlogUser represents the user data that needs to be kept in session.
 * Default implementation keeps username and role information.
 */
class BlogUser extends TDbUser
{
	/**
	 * Creates a BlogUser object based on the specified username.
	 * This method is required by TDbUser. It checks the database
	 * to see if the specified username is there. If so, a BlogUser
	 * object is created and initialized.
	 * @param string the specified username
	 * @return BlogUser the user object, null if username is invalid.
	 */
	public function createUser($username)
	{
		// use UserRecord Active Record to look for the specified username
		$userRecord=UserRecord::finder()->findByPk($username);
		if($userRecord instanceof UserRecord) // if found
		{
			$user=new BlogUser($this->Manager);
			$user->Name=$username;  // set username
			$user->Roles=($userRecord->role==1?'admin':'user'); // set role
			$user->IsGuest=false;   // the user is not a guest
			return $user;
		}
		else
			return null;
	}

	/**
	 * Checks if the specified (username, password) is valid.
	 * This method is required by TDbUser.
	 * @param string username
	 * @param string password
	 * @return boolean whether the username and password are valid.
	 */
	public function validateUser($username,$password)
	{
		// use UserRecord Active Record to look for the (username, password) pair.
		return UserRecord::finder()->findBy_username_AND_password($username,$password)!==null;
	}

	/**
	 * @return boolean whether this user is an administrator.
	 */
	public function getIsAdmin()
	{
		return $this->isInRole('admin');
	}
}

?>