<?php

class LoginUser extends TPage
{
	/**
	 * Validates whether the username and password are correct.
	 * This method responds to the TCustomValidator's OnServerValidate event.
	 * @param mixed event sender
	 * @param mixed event parameter
	 */
	public function validateUser($sender,$param)
	{
		$authManager=$this->Application->getModule('auth');
		if(!$authManager->login($this->Username->Text,$this->Password->Text))
			$param->IsValid=false;  // tell the validator that validation fails
	}

	/**
	 * Redirects the user's browser to appropriate URL if login succeeds.
	 * This method responds to the login button's OnClick event.
	 * @param mixed event sender
	 * @param mixed event parameter
	 */
	public function loginButtonClicked($sender,$param)
	{
		if($this->Page->IsValid)  // all validations succeed
		{
			// obtain the URL of the privileged page that the user wanted to visit originally
			$url=$this->Application->getModule('auth')->ReturnUrl;
			if(empty($url))  // the user accesses the login page directly
				$url=$this->Service->DefaultPageUrl;
			$this->Response->redirect($url);
		}
	}
}

?>