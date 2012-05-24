<?php

class Home extends TPage
{
	public function viewChanged($sender,$param)
	{
		if($this->MultiView->ActiveViewIndex===2)
		{
			$this->Result1->Text="Your text input is: ".$this->Memo->Text;
			$this->Result2->Text="Your color choice is: ".$this->DropDownList->SelectedValue;
		}
	}
}

?>