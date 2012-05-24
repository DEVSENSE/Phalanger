<?php

// $Id: Home.php 1405 2006-09-10 01:03:56Z wei $
class Home extends TPage
{
	public function buttonClicked($sender, $param)
	{
		if($param instanceof TCommandEventParameter)
			$sender->Text="Name: {$param->CommandName}, Param: {$param->CommandParameter}";
		else
			$sender->Text="I'm clicked";
	}

	public function buttonCallback($sender, $param)
	{
		$sender->Text .= ' using callback';
	}
}

?>