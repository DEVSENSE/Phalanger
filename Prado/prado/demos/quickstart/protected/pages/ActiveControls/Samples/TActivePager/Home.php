<?php

class Home extends TPage
{
	
	/**
	 * function to render callback and refresh the TActivePanel content
	 */
	
	public function RenderCallback($sender, $param)
	{				
		$this->TActivePanel->render($param->NewWriter);		
	}
		
	/**
	 * Returns total number of data items.
	 * In DB-driven applications, this typically requires
	 * execution of an SQL statement with COUNT function.
	 * Here we simply return a constant number.
	 */
	protected function getDataItemCount()
	{
		return 19;
	}

	/**
	 * Fetches a page of data.
	 * In DB-driven applications, this can be achieved by executing
	 * an SQL query with LIMIT clause.
	 */
	protected function getData($offset,$limit)
	{
		$data=array(
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
		return array_slice($data,$offset,$limit);
	}

	/**
	 * Determines which page of data to be displayed and
	 * populates the datalist with the fetched data.
	 */
	protected function populateData()
	{
		$offset=$this->DataList->CurrentPageIndex*$this->DataList->PageSize;
		$limit=$this->DataList->PageSize;
		if($offset+$limit>$this->DataList->VirtualItemCount)
			$limit=$this->DataList->VirtualItemCount-$offset;
		$data=$this->getData($offset,$limit);
		$this->DataList->DataSource=$data;
		$this->DataList->dataBind();
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$this->DataList->VirtualItemCount=$this->DataItemCount;
			$this->populateData();
		}
	}

	/**
	 * Event handler to the OnPageIndexChanged event of pagers.
	 */
	public function pageChanged($sender,$param)
	{
		$this->DataList->CurrentPageIndex=$param->NewPageIndex;
		$this->populateData();
	}
}

?>