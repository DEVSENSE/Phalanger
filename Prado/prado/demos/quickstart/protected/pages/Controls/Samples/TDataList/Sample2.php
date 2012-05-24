<?php

class Sample2 extends TPage
{
	private $_data=null;

	protected function getData()
	{
		if($this->_data===null)
			$this->loadData();
		return $this->_data;
	}

	protected function loadData()
	{
		// We use viewstate keep track of data.
		// In real applications, data should come from database using an SQL SELECT statement.
		// In the following tabular data, field 'id' is the primary key.
		// All update and delete operations should come with an 'id' value in order to go through.
		if(($this->_data=$this->getViewState('Data',null))===null)
		{
			$this->_data=array(
				array('id'=>'ITN001','name'=>'Motherboard','quantity'=>1,'price'=>100.00,'imported'=>true),
				array('id'=>'ITN002','name'=>'CPU','quantity'=>1,'price'=>150.00,'imported'=>true),
				array('id'=>'ITN003','name'=>'Harddrive','quantity'=>2,'price'=>80.00,'imported'=>false),
				array('id'=>'ITN004','name'=>'Sound card','quantity'=>1,'price'=>40.00,'imported'=>false),
				array('id'=>'ITN005','name'=>'Video card','quantity'=>1,'price'=>150.00,'imported'=>true),
				array('id'=>'ITN006','name'=>'Keyboard','quantity'=>1,'price'=>20.00,'imported'=>true),
				array('id'=>'ITN007','name'=>'Monitor','quantity'=>2,'price'=>300.00,'imported'=>false),
			);
			$this->saveData();
		}
	}

	protected function saveData()
	{
		$this->setViewState('Data',$this->_data);
	}

	protected function updateProduct($id,$name,$quantity,$price,$imported)
	{
		// In real applications, data should be saved to database using an SQL UPDATE statement
		if($this->_data===null)
			$this->loadData();
		$updateRow=null;
		foreach($this->_data as $index=>$row)
			if($row['id']===$id)
				$updateRow=&$this->_data[$index];
		if($updateRow!==null)
		{
			$updateRow['name']=$name;
			$updateRow['quantity']=TPropertyValue::ensureInteger($quantity);
			$updateRow['price']=TPropertyValue::ensureFloat($price);
			$updateRow['imported']=TPropertyValue::ensureBoolean($imported);
			$this->saveData();
		}
	}

	protected function deleteProduct($id)
	{
		// In real applications, data should be saved to database using an SQL DELETE statement
		if($this->_data===null)
			$this->loadData();
		$deleteIndex=-1;
		foreach($this->_data as $index=>$row)
			if($row['id']===$id)
				$deleteIndex=$index;
		if($deleteIndex>=0)
		{
			unset($this->_data[$deleteIndex]);
			$this->saveData();
		}
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$this->DataList->DataSource=$this->Data;
			$this->DataList->dataBind();
		}
	}

	public function editItem($sender,$param)
	{
		$this->DataList->SelectedItemIndex=-1;
		$this->DataList->EditItemIndex=$param->Item->ItemIndex;
		$this->DataList->DataSource=$this->Data;
		$this->DataList->dataBind();
	}

	public function cancelItem($sender,$param)
	{
		$this->DataList->SelectedItemIndex=-1;
		$this->DataList->EditItemIndex=-1;
		$this->DataList->DataSource=$this->Data;
		$this->DataList->dataBind();
	}

	public function updateItem($sender,$param)
	{
		$item=$param->Item;
		$this->updateProduct(
			$this->DataList->DataKeys[$item->ItemIndex],
			$item->ProductName->Text,
			$item->ProductQuantity->Text,
			$item->ProductPrice->Text,
			$item->ProductImported->Checked);
		$this->DataList->EditItemIndex=-1;
		$this->DataList->DataSource=$this->Data;
		$this->DataList->dataBind();
	}

	public function deleteItem($sender,$param)
	{
		$this->deleteProduct($this->DataList->DataKeys[$param->Item->ItemIndex]);
		$this->DataList->SelectedItemIndex=-1;
		$this->DataList->EditItemIndex=-1;
		$this->DataList->DataSource=$this->Data;
		$this->DataList->dataBind();
	}

	public function selectItem($sender,$param)
	{
		$this->DataList->EditItemIndex=-1;
		$this->DataList->DataSource=$this->Data;
		$this->DataList->dataBind();
	}
}

?>