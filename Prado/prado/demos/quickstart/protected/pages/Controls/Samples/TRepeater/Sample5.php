<?php

class Sample5 extends TPage
{
	protected function getData()
	{
		return array(
			array(
				'name'=>'North',
				'detail'=>array(
					array('name'=>'John','age'=>30,'position'=>'Program Manager'),
					array('name'=>'Edward','age'=>35,'position'=>'Developer'),
					array('name'=>'Walter','age'=>28,'position'=>'Developer'),
				),
			),
			array(
				'name'=>'West',
				'detail'=>array(
					array('name'=>'Cary','age'=>31,'position'=>'Senior Manager'),
					array('name'=>'Ted','age'=>25,'position'=>'Developer'),
					array('name'=>'Kevin','age'=>28,'position'=>'Developer'),
				),
			),
			array(
				'name'=>'East',
				'detail'=>array(
					array('name'=>'Shawn','age'=>30,'position'=>'Sales Manager'),
					array('name'=>'Larry','age'=>28,'position'=>'Document Writer'),
				),
			),
			array(
				'name'=>'South',
				'detail'=>array(
					array('name'=>'King','age'=>30,'position'=>'Program Manager'),
					array('name'=>'Carter','age'=>22,'position'=>'Developer'),
				),
			),
		);
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$this->Repeater->DataSource=$this->getData();
			$this->Repeater->dataBind();
		}
	}
}

?>