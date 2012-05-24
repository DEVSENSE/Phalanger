<?php

class Sample1 extends TPage
{
	protected function getData()
	{
		return array(
			array('id'=>'ITN001','name'=>'Motherboard','quantity'=>1,'price'=>100.00,'imported'=>true),
			array('id'=>'ITN002','name'=>'CPU','quantity'=>1,'price'=>150.00,'imported'=>true),
			array('id'=>'ITN003','name'=>'Harddrive','quantity'=>2,'price'=>80.00,'imported'=>true),
			array('id'=>'ITN004','name'=>'Sound card','quantity'=>1,'price'=>40.00,'imported'=>false),
			array('id'=>'ITN005','name'=>'Video card','quantity'=>1,'price'=>150.00,'imported'=>true),
			array('id'=>'ITN006','name'=>'Keyboard','quantity'=>1,'price'=>20.00,'imported'=>false),
			array('id'=>'ITN007','name'=>'Monitor','quantity'=>2,'price'=>300.00,'imported'=>true),
			array('id'=>'ITN008','name'=>'CDRW drive','quantity'=>1,'price'=>40.00,'imported'=>true),
			array('id'=>'ITN009','name'=>'Cooling fan','quantity'=>2,'price'=>10.00,'imported'=>false),
			array('id'=>'ITN010','name'=>'Video camera','quantity'=>20,'price'=>30.00,'imported'=>true),
			array('id'=>'ITN011','name'=>'Card reader','quantity'=>10,'price'=>24.00,'imported'=>true),
			array('id'=>'ITN012','name'=>'Floppy drive','quantity'=>50,'price'=>12.00,'imported'=>false),
			array('id'=>'ITN013','name'=>'CD drive','quantity'=>25,'price'=>20.00,'imported'=>true),
			array('id'=>'ITN014','name'=>'DVD drive','quantity'=>15,'price'=>80.00,'imported'=>true),
			array('id'=>'ITN015','name'=>'Mouse pad','quantity'=>50,'price'=>5.00,'imported'=>false),
			array('id'=>'ITN016','name'=>'Network cable','quantity'=>40,'price'=>8.00,'imported'=>true),
			array('id'=>'ITN017','name'=>'Case','quantity'=>8,'price'=>65.00,'imported'=>false),
			array('id'=>'ITN018','name'=>'Surge protector','quantity'=>45,'price'=>15.00,'imported'=>false),
			array('id'=>'ITN019','name'=>'Speaker','quantity'=>35,'price'=>65.00,'imported'=>false),
		);
	}

	public function buttonClicked($sender, $param)
	{
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}
}

?>