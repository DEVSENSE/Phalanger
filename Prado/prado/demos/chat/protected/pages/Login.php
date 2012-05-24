<?php

class Login extends TPage
{
	/**
	 * Check that the username is not already taken.
	 * @param TControl custom validator that created the event.
	 * @param TServerValidateEventParameter validation parameters. 
	 */
	function checkUsername($sender, $param)
	{
		$manager = $this->Application->Modules['users'];
		if($manager->usernameExists($this->username->Text))
			$param->IsValid = false;
	}

	/**
	 * Create and login a new user, then redirect to the requested page.
	 * @param TControl button control that created the event.
	 * @param TEventParameter event parameters.
	 */
	function createNewUser($sender, $param)
	{
		if($this->Page->IsValid)
		{
			$manager = $this->Application->Modules['users'];
			$manager->addNewUser($this->username->Text);
			
			//do manual login
			$user = $manager->getUser($this->username->Text);
			$auth = $this->Application->Modules['auth'];
			$auth->updateSessionUser($user);
			$this->Application->User = $user;

			$this->Response->redirect($auth->ReturnUrl);
		}
	}
}

?>