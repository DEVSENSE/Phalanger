<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		$item=$sender->Items[$param->Index];
		$this->Result->Text="You clicked $item->Text : $item->Value.";
	}
}

?>