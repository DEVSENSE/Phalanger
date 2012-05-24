<?php
// $Id: Home.php 1405 2006-09-10 01:03:56Z wei $
class Home extends TPage
{
	public function checkboxClicked($sender,$param)
	{
		$sender->Text= $sender->ClientID . " clicked";
	}

	public function checkboxCallback($sender, $param)
	{
		$sender->Text .= ' using callback';
	}
}

?>