<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$this->Result->Text='You have entered "'.$this->PasswordInput->Text.'".';
	}
}

?>