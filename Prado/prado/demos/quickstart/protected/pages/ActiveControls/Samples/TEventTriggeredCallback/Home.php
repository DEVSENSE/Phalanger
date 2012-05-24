<?php

class Home extends TPage
{
	public function txtFocused($sender,$param)
	{
		$this->label1->Text="Textbox focused";
	}

	public function txtBlurred($sender,$param)
	{
		$this->label1->Text="Textbox lost focus";
	}
}

?>