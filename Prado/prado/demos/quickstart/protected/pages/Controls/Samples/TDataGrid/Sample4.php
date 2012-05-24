<?php

Prado::using('Application.pages.Controls.Samples.TDataGrid.Sample2');

class Sample4 extends Sample2
{
	protected function sortData($data,$key)
	{
		$compare = create_function('$a,$b','if ($a["'.$key.'"] == $b["'.$key.'"]) {return 0;}else {return ($a["'.$key.'"] > $b["'.$key.'"]) ? 1 : -1;}');
		usort($data,$compare) ;
		return $data ;
	}

	public function sortDataGrid($sender,$param)
	{
		$this->DataGrid->DataSource=$this->sortData($this->Data,$param->SortExpression);
		$this->DataGrid->dataBind();
	}
}

?>