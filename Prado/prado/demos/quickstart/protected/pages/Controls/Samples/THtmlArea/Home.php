<?php

class Home extends TPage
{
	public function button1Clicked($sender,$param)
	{
		$this->Result1->Text="You have entered: ".$this->HtmlArea1->Text;
	}

	public function button2Clicked($sender,$param)
	{
		$this->Result2->Text="You have entered: ".$this->HtmlArea2->Text;
	}

	public function button3Clicked($sender,$param)
	{
		$this->Result3->Text="You have entered: ".$this->HtmlArea3->Text;
	}
}

?>