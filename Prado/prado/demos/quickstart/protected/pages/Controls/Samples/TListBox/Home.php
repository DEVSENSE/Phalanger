<?php

class Home extends TPage
{
	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$data=array('item 1','item 2','item 3','item 4');
			$this->DBListBox1->DataSource=$data;
			$this->DBListBox1->dataBind();

			$data=array('key 1'=>'item 1','key 2'=>'item 2',
						'key 3'=>'item 3','key 4'=>'item 4');
			$this->DBListBox2->DataSource=$data;
			$this->DBListBox2->dataBind();

			$data=array(
				array('id'=>'001','name'=>'John','age'=>31),
				array('id'=>'002','name'=>'Mary','age'=>30),
				array('id'=>'003','name'=>'Cary','age'=>20));
			$this->DBListBox3->DataSource=$data;
			$this->DBListBox3->dataBind();
		}
	}

	public function DBListBox1Changed($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->DBListBox1Result);
	}

	public function DBListBox2Changed($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->DBListBox2Result);
	}

	public function DBListBox3Changed($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->DBListBox3Result);
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

	public function buttonClicked($sender,$param)
	{
		$this->collectSelectionResult($this->ListBox1,$this->SelectionResult2);
	}

	public function multiSelectionChanged($sender,$param)
	{
		$this->collectSelectionResult($sender,$this->MultiSelectionResult);
	}

	public function buttonClicked2($sender,$param)
	{
		$this->collectSelectionResult($this->ListBox2,$this->MultiSelectionResult2);
	}
}

?>