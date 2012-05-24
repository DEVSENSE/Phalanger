<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$this->Result->Text="Your post value is : ".$param->PostBackValue;
	}
}

?>