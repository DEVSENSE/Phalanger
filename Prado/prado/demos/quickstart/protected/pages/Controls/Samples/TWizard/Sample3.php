<?php

class Sample3 extends TPage
{
	public function wizard3Completed($sender,$param)
	{
		if($this->StudentCheckBox->Checked)
		{
			$str="You are a college student.<br/>";
			$str.="You are in major: ".$this->DropDownList11->SelectedValue."<br/>";
			$str.="Your favorite sport is: ".$this->DropDownList22->SelectedValue;
		}
		else
			$str="Your favorite sport is: ".$this->DropDownList22->SelectedValue;
		$this->Wizard3Result->Text=$str;
	}

	public function wizard3NextStep($sender,$param)
	{
		if($param->CurrentStepIndex===0 && !$this->StudentCheckBox->Checked)
			$this->Wizard3->ActiveStepIndex=2;
	}
}

?>