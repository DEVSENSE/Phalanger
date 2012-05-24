<?php

class Home extends TPage
{
	private $_client=null;

	protected function getSoapClient()
	{
		if($this->_client===null)
		{
			$wsdlUri=$this->Request->AbsoluteApplicationUrl.'?soap=calculator.wsdl';
			$this->_client=new SoapClient($wsdlUri);
		}
		return $this->_client;
	}

	public function computeButtonClicked($sender,$param)
	{
		$number1=TPropertyValue::ensureInteger($this->Number1->Text);
		$number2=TPropertyValue::ensureInteger($this->Number2->Text);
		$this->AdditionResult->Text=$this->getSoapClient()->add($number1+$number2);
	}

	public function highlightButtonClicked($sender, $param)
	{
		$this->HighlightResult->Text=$this->getSoapClient()->highlight(file_get_contents(__FILE__));
	}

}

?>