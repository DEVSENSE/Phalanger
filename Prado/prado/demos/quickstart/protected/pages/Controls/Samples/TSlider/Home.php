<?php
class Home extends TPage 
{
	public function onInit ($param)
	{
		parent::onInit($param);
	}
	public function submit1 ($sender, $param)
	{
		$this->slider1Result->setText('Slider Value : '.$this->slider1->getValue());	
	}
	
	public function slider2Changed ($sender, $param)
	{
		$this->slider2Result->setText('onSliderChanged, Value : '.$sender->getValue());
	}
	
	public function slider3Changed ($sender, $param)
	{
		$this->slider3Result->setText('onSliderChanged, Value : '.$sender->getValue());
	}
}
?>