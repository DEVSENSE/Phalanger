<?php

class Home extends TPage
{
	public function onInit($param)
	{
		parent::onInit($param);
		$label=new TLabel;
		$label->Text='dynamic';
		$label->BackColor='silver';
		$this->PlaceHolder1->Controls[]=$label;
		$this->PlaceHolder1->Controls[]=' content';
	}
}

?>