<?php

class Home extends TPage
{
	public function radiobuttonClicked($sender,$param)
	{
		$sender->Text="I'm clicked";
	}

	public function selectRadioButton($sender,$param)
	{
		$selection='';
		if($this->Radio1->Checked)
			$selection.='1';
		if($this->Radio2->Checked)
			$selection.='2';
		if($this->Radio3->Checked)
			$selection.='3';
		if($this->Radio4->Checked)
			$selection.='4';
		if($selection==='')
			$selection='empty';
		$this->Result->Text='Your selection is '.$selection;
	}
}

?>