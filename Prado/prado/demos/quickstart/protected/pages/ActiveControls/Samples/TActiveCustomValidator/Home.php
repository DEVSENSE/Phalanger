<?php

class Home extends TPage
{
	public function validator1_onvalidate($sender, $param)
	{
		$param->IsValid = $this->textbox1->Text == 'Prado';
	}
}

?>