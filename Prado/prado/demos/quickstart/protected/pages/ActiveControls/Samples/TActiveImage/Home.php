<?php

// $Id: Home.php 1405 2006-09-10 01:03:56Z wei $
class Home extends TPage
{
	public function buttonClicked($sender, $param)
	{
		$this->imageTest->ImageUrl=$this->publishAsset("hello_world.gif");
	}

	public function buttonClicked2($sender, $param)
	{
		$this->imageTest->ImageUrl=$this->publishAsset("hello_world2.gif");
	}
}

?>