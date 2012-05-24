<?php

class Home extends TPage
{
	public function callback1_Requested($sender,$param)
	{
		$this->label1->Text="You clicked the div, didn't you?";
	}

	public function buttonClicked($sender,$param)
	{
		$this->label1->Text="This is a label";
	}

	public function callback2_Requested($sender,$param)
	{
		$parameters=$param->CallbackParameter;
		$this->labelParam1->Text = THttpUtility::htmlEncode($parameters->Param1);
		$this->labelParam2->Text = THttpUtility::htmlEncode($parameters->Param2);
		$this->labelParam3->Text = THttpUtility::htmlEncode($parameters->Param3);
	}
}

?>