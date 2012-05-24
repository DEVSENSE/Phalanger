<?php

class EditUser extends TPage
{
	/**
	 * Initializes the inputs with existing user data.
	 * This method is invoked by the framework when the page is being initialized.
	 * @param mixed event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		if(!$this->IsPostBack)  // if the page is initially requested
		{
			// Retrieves the existing user data. This is equivalent to:
			// $userRecord=$this->getUserRecord();
			$userRecord=$this->UserRecord;

			// Populates the input controls with the existing user data
			$this->Username->Text=$userRecord->username;
			$this->Email->Text=$userRecord->email;
			$this->Role->SelectedValue=$userRecord->role;
			$this->FirstName->Text=$userRecord->first_name;
			$this->LastName->Text=$userRecord->last_name;
		}
	}

	/**
	 * Saves the user account if all inputs are valid.
	 * This method responds to the OnClick event of the "save" button.
	 * @param mixed event sender
	 * @param mixed event parameter
	 */
	public function saveButtonClicked($sender,$param)
	{
		if($this->IsValid)  // when all validations succeed
		{
			// Retrieves the existing user data. This is equivalent to:
			$userRecord=$this->UserRecord;

			// Fetches the input data
			$userRecord->username=$this->Username->Text;
			// update password when the input is not empty
			if(!empty($this->Password->Text))
				$userRecord->password=$this->Password->Text;
			$userRecord->email=$this->Email->Text;
			// update the role if the current user is an administrator
			if($this->User->IsAdmin)
				$userRecord->role=(int)$this->Role->SelectedValue;
			$userRecord->first_name=$this->FirstName->Text;
			$userRecord->last_name=$this->LastName->Text;

			// saves to the database via Active Record mechanism
			$userRecord->save();

			// redirects the browser to the homepage
			$this->Response->redirect($this->Service->DefaultPageUrl);
		}
	}

	/**
	 * Returns the user data to be editted.
	 * @return UserRecord the user data to be editted.
	 * @throws THttpException if the user data is not found.
	 */
	protected function getUserRecord()
	{
		// the user to be editted is the currently logged-in user
		$username=$this->User->Name;
		// if the 'username' GET var is not empty and the current user
		// is an administrator, we use the GET var value instead.
		if($this->User->IsAdmin && $this->Request['username']!==null)
			$username=$this->Request['username'];

		// use Active Record to look for the specified username
		$userRecord=UserRecord::finder()->findByPk($username);
		if(!($userRecord instanceof UserRecord))
			throw new THttpException(500,'Username is invalid.');
		return $userRecord;
	}
}

?>