<?php
Prado::using('System.Web.UI.ActiveControls.*');
class Home extends TPage
{
	public function convert_clicked($sender, $param)
	{
		if($this->Page->IsValid)
		{
			$rate = floatval($this->currencyRate->Text);
			$dollars = floatval($this->dollars->Text);
			$this->total->Text = $rate * $dollars;
		}
	}
}
?>