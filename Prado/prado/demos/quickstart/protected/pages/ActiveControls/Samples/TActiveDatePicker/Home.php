<?php

class Home extends TPage
{
	public function dateChanged($sender, $param)
	{
		$this->label1->Text = date("r", $this->date1->TimeStamp);
	}
}

?>