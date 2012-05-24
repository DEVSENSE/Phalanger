<?php

class Home extends TPage
{
	public function onLoad($param)
	{
		$username = $this->Application->User->Name;
		if(!$this->Application->Modules['users']->usernameExists($username))
		{
			$auth = $this->Application->Modules['auth'];
			$auth->logout();

			//redirect to login page.
			$this->Response->Redirect($this->Service->ConstructUrl($auth->LoginPage));
		}
	}

	function processMessage($sender, $param)
	{
		if(strlen($this->userinput->Text) > 0)
		{
			$record = new ChatBufferRecord();
			$record->message = $this->userinput->Text;
			$record->from_user = $this->Application->User->Name;
			$record->saveMessage();
			$this->userinput->Text = '';
			$this->refresh($sender, $param);
			$this->CallbackClient->focus($this->userinput);
		}
	}

	function refresh($sender, $param)
	{
		//refresh the message list
		$content = ChatBufferRecord::finder()->getUserMessages($this->Application->User->Name);
		if(strlen($content) > 0)
		{
			$client = $this->Page->CallbackClient;
			$anchor = (string)time();
			$content .= "<a href=\"#\" id=\"{$anchor}\"> </a>";
			$client->appendContent("messages", $content);
			$client->focus($anchor);
		}

		//refresh the user list
		$lastUpdate = $this->getViewState('userList','');
		$users = ChatUserRecord::finder()->getUserList();
		if($lastUpdate != $users)
		{
			$this->Page->CallbackClient->update('users', $users);
			$this->setViewstate('userList', $users);
		}
	}
}

?>