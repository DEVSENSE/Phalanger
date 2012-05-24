<?php

// $Id: Home.php -1   $
class Home extends TPage
{
	public function buttonCallback ($sender, $param)
	{
		switch($this->radio1->SelectedValue)
		{
			case 1:
				$this->getCallbackClient()->evaluateScript("<script> alert('something'); </script>");
				break;
			case 2:
				$this->getCallbackClient()->check($this->check1, !$this->check1->Checked);
				break;
			case 3:
				$this->getCallbackClient()->hide($this->label1);
				break;
			case 4:
				$this->getCallbackClient()->show($this->label1);
				break;
			case 5:
				$this->getCallbackClient()->focus($this->txt1);
				break;
		}
	}
}

?>