<?php

Prado::using('Application.pages.Controls.Samples.TDataGrid.Sample1');

class Sample6 extends Sample1
{
	/**
	 * Returns a subset of data.
	 * In MySQL database, this can be replaced by LIMIT clause
	 * in an SQL select statement.
	 * @param integer the starting index of the row
	 * @param integer number of rows to be returned
	 * @return array subset of data
	 */
	protected function getDataRows($offset,$rows)
	{
		$data=$this->getData();
		$page=array();
		for($i=0;$i<$rows;++$i)
		{
			if($offset+$i<$this->getRowCount())
				$page[$i]=$data[$offset+$i];
		}
		return $page;
	}

	/**
	 * Returns total number of data rows.
	 * In real DB applications, this may be replaced by an SQL select
	 * query with count().
	 * @return integer total number of data rows
	 */
	protected function getRowCount()
	{
		return 19;
	}

	public function onLoad($param)
	{
		if(!$this->IsPostBack)
		{
			$this->DataGrid->DataSource=$this->getDataRows(0,$this->DataGrid->PageSize);
			$this->DataGrid->dataBind();
		}
	}

	public function changePage($sender,$param)
	{
		$this->DataGrid->CurrentPageIndex=$param->NewPageIndex;
		$offset=$param->NewPageIndex*$this->DataGrid->PageSize;
		$this->DataGrid->DataSource=$this->getDataRows($offset,$this->DataGrid->PageSize);
		$this->DataGrid->dataBind();
	}
}

?>