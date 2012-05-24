<?php

class Sample5 extends TPage
{
	public function wizardCompleted($sender,$param)
	{
		$this->Result1->Text="Your favorite color is: " . $this->DropDownList1->SelectedValue;
		$this->Result2->Text="Your favorite sport is: " . $this->Step2->DropDownList2->SelectedValue;
	}
}

?>