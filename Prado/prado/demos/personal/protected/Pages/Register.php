<?php

class Register extends TPage
{
	public function checkUsername($sender,$param)
	{
		// set $param->IsValid to false if the username is already taken
	}

	public function createUser($sender,$param)
	{
		if($this->IsValid)
		{
			// create new user account
		}
	}
}

?>