<?php

Prado::using('Application.pages.Controls.Samples.LabeledTextBox1.LabeledTextBox');

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$sender->Text=$this->Input->TextBox->Text;
	}
}

?>