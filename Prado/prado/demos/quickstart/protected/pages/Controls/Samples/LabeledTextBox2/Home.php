<?php

Prado::using('Application.pages.Controls.Samples.LabeledTextBox2.LabeledTextBox');

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$sender->Text=$this->Input->TextBox->Text;
	}
}

?>