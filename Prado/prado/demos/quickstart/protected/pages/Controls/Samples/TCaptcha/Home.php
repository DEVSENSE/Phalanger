<?php

class Home extends TPage
{
	public function onInit($param)
	{
		if(!$this->IsPostBack)
		{
			$this->CaptchaList->DataSource=range(0,63);
			$this->CaptchaList->dataBind();
		}
	}

	public function regenerateToken($sender,$param)
	{
		$this->Captcha->regenerateToken();
		$this->SubmitButton->Text="Submit";
	}

	public function buttonClicked($sender,$param)
	{
		if($this->IsValid)
			$sender->Text="You passed!";
	}
}

?>