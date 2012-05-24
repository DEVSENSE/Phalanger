<?php

class Home extends TPage
{
	public function checkboxClicked($sender,$param)
	{
		$sender->Text="I'm clicked";
	}
}

?>