<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		if($param instanceof TCommandEventParameter)
			$sender->Text="Name: {$param->CommandName}, Param: {$param->CommandParameter}";
		else
			$sender->Text="I'm clicked";
	}
}

?>