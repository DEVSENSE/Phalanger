<?php

class NewUser extends TPage
{
	/**
	 * Checks whether the username exists in the database.
	 * This method responds to the OnServerValidate event of username's custom validator.
	 * @param mixed event sender
	 * @param mixed event parameter
	 */
	public function checkUsername($sender,$param)
	{
		// valid if the username is not found in the database
		$param->IsValid=UserRecord::finder()->findByPk($this->Username->Text)===null;
	}

	/**
	 * Creates a new user account if all inputs are valid.
	 * This method responds to the OnClick event of the "create" button.
	 * @param mixed event sender
	 * @param mixed event parameter
	 */
	public function createButtonClicked($sender,$param)
	{
		if($this->IsValid)  // when all validations succeed
		{
			// populates a UserRecord object with user inputs
			$userRecord=new UserRecord;
			$userRecord->username=$this->Username->Text;
			$userRecord->password=$this->Password->Text;
			$userRecord->email=$this->Email->Text;
			$userRecord->role=(int)$this->Role->SelectedValue;
			$userRecord->first_name=$this->FirstName->Text;
			$userRecord->last_name=$this->LastName->Text;

			// saves to the database via Active Record mechanism
			$userRecord->save();

			// redirects the browser to the homepage
			$this->Response->redirect($this->Service->DefaultPageUrl);
		}
	}
}

?>