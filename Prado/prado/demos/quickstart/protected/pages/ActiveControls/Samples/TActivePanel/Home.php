<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$this->check1->Checked = !$this->check1->Checked;
		if($this->txt1->Text=="")
			$this->txt1->Text="changes happens";
		else
			$this->txt1->Text="";
		if($this->label1->Text=="")
			$this->label1->Text="label has changed, too";
		else
			$this->label1->Text="";

		$this->panel1->render($param->NewWriter);
	}
}

?>