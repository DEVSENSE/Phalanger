<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		if($param instanceof TCallbackEventParameter)
			$sender->Text="Callback Parameter: {$param->CallbackParameter}";
		else
			$sender->Text="I'm clicked";
	}
}

?>