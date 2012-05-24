<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$sender->Text='Hello world!';
	}
}

?>