<?php

Prado::using('System.Collections.TDummyDataSource');

class Sample4 extends TPage
{
	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			// use a dummy data source to create 3 repeater items
			$this->Repeater->DataSource=new TDummyDataSource(3);
			$this->Repeater->dataBind();
		}
	}

	public function itemCreated($sender,$param)
	{
		// $param->Item refers to the newly created repeater item
		$param->Item->Style="width:300px; margin:10px; margin-left:0px";
	}

	public function buttonClicked($sender,$param)
	{
		$links=array();
		foreach($this->Repeater->Items as $textBox)
		{
			if($textBox->Text!=='')
				$links[]=$textBox->Text;
		}
		$this->Repeater2->DataSource=$links;
		$this->Repeater2->dataBind();
	}
}

?>