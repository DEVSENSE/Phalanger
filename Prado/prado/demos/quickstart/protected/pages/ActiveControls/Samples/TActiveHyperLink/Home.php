<?php

class Home extends TPage
{
	function button1_clicked($sender, $param)
	{
		$this->link1->NavigateUrl = 'http://www.google.com';
	}

	function button2_clicked($sender, $param)
	{
		$this->link2->Target = '_self';
	}

	function button3_clicked($sender, $param)
	{
		$this->link3->Text = 'PradoSoft.com';
	}

	function button4_clicked($sender, $param)
	{
		$img = $this->publishFilePath(dirname(__FILE__).'/hello_world.gif');
		$this->link4->ImageUrl = $img;
	}
}
?>