<?php

class Sample3 extends TPage
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
		// In the following tabular data, field 'ISBN' is the primary key.
		// All update and delete operations should come with an 'id' value in order to go through.
		if(($this->_data=$this->getViewState('Data',null))===null)
		{
			$this->_data=array(
				array(
					'ISBN'=>'0596007124',
					'title'=>'Head First Design Patterns',
					'publisher'=>'O\'Reilly Media, Inc.',
					'price'=>29.67,
					'instock'=>true,
					'rating'=>4,
				),
				array(
					'ISBN'=>'0201633612',
					'title'=>'Design Patterns: Elements of Reusable Object-Oriented Software',
					'publisher'=>'Addison-Wesley Professional',
					'price'=>47.04,
					'instock'=>true,
					'rating'=>5,
				),
				array(
					'ISBN'=>'0321247140',
					'title'=>'Design Patterns Explained : A New Perspective on Object-Oriented Design',
					'publisher'=>'Addison-Wesley Professional',
					'price'=>37.49,
					'instock'=>true,
					'rating'=>4,
				),
				array(
					'ISBN'=>'0201485672',
					'title'=>'Refactoring: Improving the Design of Existing Code',
					'publisher'=>'Addison-Wesley Professional',
					'price'=>47.14,
					'instock'=>true,
					'rating'=>3,
				),
				array(
					'ISBN'=>'0321213351',
					'title'=>'Refactoring to Patterns',
					'publisher'=>'Addison-Wesley Professional',
					'price'=>38.49,
					'instock'=>true,
					'rating'=>2,
				),
				array(
					'ISBN'=>'0735619670',
					'title'=>'Code Complete',
					'publisher'=>'Microsoft Press',
					'price'=>32.99,
					'instock'=>false,
					'rating'=>4,
				),
				array(
					'ISBN'=>'0321278658 ',
					'title'=>'Extreme Programming Explained : Embrace Change',
					'publisher'=>'Addison-Wesley Professional',
					'price'=>34.99,
					'instock'=>true,
					'rating'=>3,
				),
			);
			$this->saveData();
		}
	}

	protected function saveData()
	{
		$this->setViewState('Data',$this->_data);
	}

	protected function updateBook($isbn,$title,$publisher,$price,$instock,$rating)
	{
		// In real applications, data should be saved to database using an SQL UPDATE statement
		if($this->_data===null)
			$this->loadData();
		$updateRow=null;
		foreach($this->_data as $index=>$row)
			if($row['ISBN']===$isbn)
				$updateRow=&$this->_data[$index];
		if($updateRow!==null)
		{
			$updateRow['title']=$title;
			$updateRow['publisher']=$publisher;
			$updateRow['price']=TPropertyValue::ensureFloat(ltrim($price,'$'));
			$updateRow['instock']=TPropertyValue::ensureBoolean($instock);
			$updateRow['rating']=TPropertyValue::ensureInteger($rating);
			$this->saveData();
		}
	}

	protected function deleteBook($isbn)
	{
		// In real applications, data should be saved to database using an SQL DELETE statement
		if($this->_data===null)
			$this->loadData();
		$deleteIndex=-1;
		foreach($this->_data as $index=>$row)
			if($row['ISBN']===$isbn)
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
			$this->DataGrid->DataSource=$this->Data;
			$this->DataGrid->dataBind();
		}
	}

	public function itemCreated($sender,$param)
	{
		$item=$param->Item;
		if($item->ItemType==='EditItem')
		{
			// set column width of textboxes
			$item->BookTitleColumn->TextBox->Columns=40;
			$item->PriceColumn->TextBox->Columns=5;
		}
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem' || $item->ItemType==='EditItem')
		{
			// add an aleart dialog to delete buttons
			$item->DeleteColumn->Button->Attributes->onclick='if(!confirm(\'Are you sure?\')) return false;';
		}
	}

	public function editItem($sender,$param)
	{
		$this->DataGrid->EditItemIndex=$param->Item->ItemIndex;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}

	public function saveItem($sender,$param)
	{
		$item=$param->Item;
		$this->updateBook(
			$this->DataGrid->DataKeys[$item->ItemIndex],	// ISBN
			$item->BookTitleColumn->TextBox->Text,			// title
			$item->PublisherColumn->TextBox->Text,			// publisher
			$item->PriceColumn->TextBox->Text,				// price
			$item->InStockColumn->CheckBox->Checked,		// instock
			$item->RatingColumn->DropDownList->SelectedValue		// rating
			);
		$this->DataGrid->EditItemIndex=-1;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}

	public function cancelItem($sender,$param)
	{
		$this->DataGrid->EditItemIndex=-1;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}

	public function deleteItem($sender,$param)
	{
		$this->deleteBook($this->DataGrid->DataKeys[$param->Item->ItemIndex]);
		$this->DataGrid->EditItemIndex=-1;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}
}

?>