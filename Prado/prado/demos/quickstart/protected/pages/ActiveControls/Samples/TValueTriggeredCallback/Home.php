<?php

class Home extends TPage
{
	public function checkTxtValue($sender,$param)
	{
		$this->label1->Text=date("[H:i:s]")." Textbox changed";
	}
}

?>