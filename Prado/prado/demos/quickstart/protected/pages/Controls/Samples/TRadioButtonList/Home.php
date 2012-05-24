<?php

class Home extends TPage
{
	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$data=array('item 1','item 2','item 3','item 4');
			$this->DBRadioButtonList1->DataSource=$data;
			$this->DBRadioButtonList1->dataBind();

			$data=array('key 1'=>'item 1','key 2'=>'item 2',
						'key 3'=>'item 3','key 4'=>'item 4');
			$this->DBRadioButtonList2->DataSource=$data;
			$this->DBRadioButtonList2->dataBind();

			$data=array(
				array('id'=>'001','name'=>'John','age'=>31),
				array('id'=>'002','name'=>'Mary','age'=>30),
				array('id'=>'003','name'=>'Cary','age'=>20));
			$this->DBRadioButtonList3->DataSource=$data;
			$this->DBRadioButtonList3->dataBind();
		}
	}

	protected function collectSelectionResult($input,$output)
	{
		$indices=$input->SelectedIndices;
		$result='';
		foreach($indices as $index)
		{
			$item=$input->Items[$index];
			$result.="(Index: $index, Value: $item->Value, Text: $item->Text)";
		}
		if($result==='')
			$output->Text='Your selection is empty.';
		else
			$output->Text='Your selection is: '.$result;
	}

	public function buttonClicked($sender,$param)
	{
		$this->collectSelectionResult($this->RadioButtonList,$this->SelectionResult);
	}

	public function selectionChanged($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->SelectionResult2);
	}

	public function DBRadioButtonList1Changed($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->DBRadioButtonList1Result);
	}

	public function DBRadioButtonList2Changed($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->DBRadioButtonList2Result);
	}

	public function DBRadioButtonList3Changed($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->DBRadioButtonList3Result);
	}
}

?>