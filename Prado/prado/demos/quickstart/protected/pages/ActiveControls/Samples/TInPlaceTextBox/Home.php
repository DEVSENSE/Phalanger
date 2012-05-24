<?php

class Home extends TPage
{
	public function textChanged($sender,$param)
	{
		$this->label1->Text=$this->txt1->Text;
	}
}

?>