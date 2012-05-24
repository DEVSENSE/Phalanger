<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$this->Result->Text="You have clicked on '$sender->Text'.";
	}
}

?>