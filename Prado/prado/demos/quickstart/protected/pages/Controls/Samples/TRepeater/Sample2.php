<?php

class Sample2 extends TPage
{
	protected function getMasterData()
	{
		return array('North','West','East','South');
	}

	protected function getDetailData($region)
	{
		static $data=array(
			'North'=>array(
				array('name'=>'John','age'=>30,'position'=>'Program Manager'),
				array('name'=>'Edward','age'=>35,'position'=>'Developer'),
				array('name'=>'Walter','age'=>28,'position'=>'Developer'),
			),
			'West'=>array(
				array('name'=>'Cary','age'=>31,'position'=>'Senior Manager'),
				array('name'=>'Ted','age'=>25,'position'=>'Developer'),
				array('name'=>'Kevin','age'=>28,'position'=>'Developer'),
			),
			'East'=>array(
				array('name'=>'Shawn','age'=>30,'position'=>'Sales Manager'),
				array('name'=>'Larry','age'=>28,'position'=>'Document Writer'),
			),
			'South'=>array(
				array('name'=>'King','age'=>30,'position'=>'Program Manager'),
				array('name'=>'Carter','age'=>22,'position'=>'Developer'),
			),
		);
		return $data[$region];
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$this->Repeater->DataSource=$this->getMasterData();
			$this->Repeater->dataBind();
		}
	}

	public function dataBindRepeater2($sender,$param)
	{
		$item=$param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
		{
			$item->Repeater2->DataSource=$this->getDetailData($item->DataItem);
			$item->Repeater2->dataBind();
		}
	}

	public function repeaterItemCreated($sender,$param)
	{
		static $itemIndex=0;
		$item=$param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
		{
			$item->Cell->BackColor=$itemIndex%2 ? "#6078BF" : "#809FFF";
			$item->Cell->ForeColor='white';
			$itemIndex++;
		}
	}

	public function repeater2ItemCreated($sender,$param)
	{
		static $itemIndex=0;
		$item=$param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
		{
			$item->Row->BackColor=$itemIndex%2 ? "#BFCFFF" : "#E6ECFF";
			$itemIndex++;
		}
	}
}

?>