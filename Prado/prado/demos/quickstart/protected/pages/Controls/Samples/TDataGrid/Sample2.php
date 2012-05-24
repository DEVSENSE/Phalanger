<?php

class Sample2 extends TPage
{
	protected function getData()
	{
		return array(
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
				'ISBN'=>'0321278658',
				'title'=>'Extreme Programming Explained : Embrace Change',
				'publisher'=>'Addison-Wesley Professional',
				'price'=>34.99,
				'instock'=>true,
				'rating'=>3,
			),
		);
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

	public function toggleColumnVisibility($sender,$param)
	{
		foreach($this->DataGrid->Columns as $index=>$column)
			$column->Visible=$sender->Items[$index]->Selected;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}
}

?>