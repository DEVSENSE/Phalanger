<?php

class Sample3 extends TPage
{
	protected function getProducts()
	{
		return array(
			array('id'=>'ITN001','name'=>'Motherboard','category'=>'CAT004','price'=>100.00,'imported'=>true),
			array('id'=>'ITN002','name'=>'CPU','category'=>'CAT004','price'=>150.00,'imported'=>true),
			array('id'=>'ITN003','name'=>'Harddrive','category'=>'CAT003','price'=>80.00,'imported'=>true),
			array('id'=>'ITN006','name'=>'Keyboard','category'=>'CAT002','price'=>20.00,'imported'=>false),
			array('id'=>'ITN008','name'=>'CDRW drive','category'=>'CAT003','price'=>40.00,'imported'=>true),
			array('id'=>'ITN009','name'=>'Cooling fan','category'=>'CAT001','price'=>10.00,'imported'=>false),
			array('id'=>'ITN012','name'=>'Floppy drive','category'=>'CAT003','price'=>12.00,'imported'=>false),
			array('id'=>'ITN013','name'=>'CD drive','category'=>'CAT003','price'=>20.00,'imported'=>true),
			array('id'=>'ITN014','name'=>'DVD drive','category'=>'CAT003','price'=>80.00,'imported'=>true),
			array('id'=>'ITN015','name'=>'Mouse pad','category'=>'CAT001','price'=>5.00,'imported'=>false),
		);
	}

	protected function getCategories()
	{
		return array(
			array('id'=>'CAT001','name'=>'Accessories'),
			array('id'=>'CAT002','name'=>'Input Devices'),
			array('id'=>'CAT003','name'=>'Drives'),
			array('id'=>'CAT004','name'=>'Barebone'),
		);
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$this->Repeater->DataSource=$this->Products;
			$this->Repeater->dataBind();
		}
	}

	public function repeaterDataBound($sender,$param)
	{
		$item=$param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
		{
			$item->ProductCategory->DataSource=$this->Categories;
			$item->ProductCategory->DataTextField='name';
			$item->ProductCategory->DataValueField='id';
			$item->ProductCategory->dataBind();
			$item->ProductCategory->SelectedValue=$item->DataItem['category'];
		}
	}

	public function saveInput($sender,$param)
	{
		if($this->IsValid)
		{
			$index=0;
			$products=$this->Products;
			$data=array();
			foreach($this->Repeater->Items as $item)
			{
				$item=array(
					'id'=>$products[$index]['id'],
					'name'=>$item->ProductName->Text,
					'category'=>$item->ProductCategory->SelectedItem->Text,
					'price'=>TPropertyValue::ensureFloat($item->ProductPrice->Text),
					'imported'=>$item->ProductImported->Checked,
				);
				$data[]=$item;
				$index++;
			}
			$this->Repeater2->DataSource=$data;
			$this->Repeater2->dataBind();
		}
	}
}

?>