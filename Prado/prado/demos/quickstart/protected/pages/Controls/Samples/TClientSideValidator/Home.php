<?php

class Home extends TPage
{
	function check_validate($sender, $params)
	{
		$sender->enabled = $this->check1->checked;
	}

	function onPreRender($param)
	{
		//always re-enable the validator2 so as to display the client-side validator
		$this->validator2->enabled=true;
	}
}

?>