<?php

class Home extends TPage
{
	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack && !$this->IsCallback)
		{
			$this->resetClicked(null,null);
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

	public function selectionChanged($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->SelectionResult);
	}

	public function buttonClicked($sender, $param)
	{
		$data=array();
		for($i = 0; $i <= $this->box1->Items->Count; $i++)
			$data[$i]="Item number #".$i;
		$this->box1->DataSource=$data;
		$this->box1->dataBind();
		$this->label1->Text="Total ".count($data)." items";
	}

	public function resetClicked($sender, $param)
	{
		$data=array('item 1','item 2','item 3','item 4');
		$this->box2->DataSource=$data;
		$this->box2->dataBind();
		$this->label2->Text="ListBox has been reset";
	}

	public function clearClicked($sender, $param)
	{
		$this->box2->DataSource=array();
		$this->box2->dataBind();
		$this->label2->Text="ListBox cleared";
	}
}

?>