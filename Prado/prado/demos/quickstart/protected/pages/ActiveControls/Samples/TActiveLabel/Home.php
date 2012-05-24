<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$this->label1->Text="You clicked the button, didn't you?";
	}

	public function buttonClicked2($sender,$param)
	{
		$this->label2->Text=THttpUtility::htmlEncode($this->txt2->Text);
	}
}

?>