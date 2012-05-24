<?php
/**
 * UserCreate page class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: UserCreate.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 */

/**
 * Create new user wizard page class. Validate that the usernames are unique and
 * set the new user credentials as the current application credentials.
 * 
 * If logged in as admin, the user role can be change during creation.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: UserCreate.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 * @since 3.1
 */
class UserCreate extends TPage
{
	/**
	 * Sets the default new user roles, default role is set in config.xml
	 */
	public function onLoad($param)
	{
		if(!$this->IsPostBack)
		{
			$this->role->SelectedValue = 	
				$this->Application->Parameters['NewUserRoles'];
		}
	}
	
	/**
	 * Verify that the username is not taken.
	 * @param TControl custom validator that created the event.
	 * @param TServerValidateEventParameter validation parameters.
	 */
	public function checkUsername($sender, $param)
	{
		$userDao = $this->Application->Modules['daos']->getDao('UserDao');
		if($userDao->usernameExists($this->username->Text))
		{
			$param->IsValid = false;
			$sender->ErrorMessage = 
				"The user name is already taken, try '{$this->username->Text}01'";
		}
	}
	
	/**
	 * Skip the role assignment step if not admin.
	 */
	public function userWizardNextStep($sender, $param)
	{
		if($param->CurrentStepIndex == 0)
		{
			//create user with admin credentials
			if(!$this->User->isInRole('admin'))
			{
				$this->createNewUser($sender, $param);
				$param->NextStepIndex = 2;
			}
		}
	}
	
	/**
	 * Create a new user if all data entered are valid.
	 * The default user roles are obtained from "config.xml". The new user
	 * details is saved to the database and the new credentials are used as the
	 * application user. The user is redirected to the requested page.
	 * @param TControl button control that created the event.
	 * @param TEventParameter event parameters.
	 */
	public function createNewUser($sender, $param)
	{
		if($this->IsValid)
		{
			$newUser = new TimeTrackerUser($this->User->Manager);
			$newUser->EmailAddress = $this->email->Text;
			$newUser->Name = $this->username->Text;
			$newUser->IsGuest = false;
			$newUser->Roles = $this->role->SelectedValue;
	
			//save the user
			$userDao = $this->Application->Modules['daos']->getDao('UserDao');
			$userDao->addNewUser($newUser, $this->password->Text);
	
			//update the user credentials if not admin
			if(!$this->User->isInRole('admin'))
			{
				$auth = $this->Application->getModule('auth');
				$auth->updateCredential($newUser);
			}
		}
	}
	
	/**
	 * Continue with requested page.
	 */
	public function wizardCompleted($sender, $param)
	{
		//return to requested page
		$auth = $this->Application->getModule('auth');
		$this->Response->redirect($auth->getReturnUrl());
	}
}

?>