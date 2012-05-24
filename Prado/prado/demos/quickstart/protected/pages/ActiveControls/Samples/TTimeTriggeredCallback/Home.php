<?php

class Home extends TPage
{
	public function startClicked($sender,$param)
	{
		$this->time1->startTimer();
	}

	public function stopClicked($sender,$param)
	{
		$this->time1->stopTimer();
	}

	public function timerCallback($sender,$param)
	{
		$this->label1->Text="Current time is ".date('H:i:s');
	}

}

?>