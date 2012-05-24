<?php

class Home extends TPage
{
	public function onLoad($param)
	{
		$this->Table->GridLines='Both';

		$row=new TTableRow;
		$this->Table->Rows[]=$row;

		$cell=new TTableHeaderCell;
		$cell->Text='Header 1';
		$row->Cells[]=$cell;

		$cell=new TTableHeaderCell;
		$cell->Text='Header 2';
		$row->Cells[]=$cell;

		$row=new TTableRow;
		$this->Table->Rows[]=$row;

		$cell=new TTableCell;
		$cell->Text='Cell 1';
		$row->Cells[]=$cell;

		$cell=new TTableCell;
		$cell->Text='Cell 2';
		$row->Cells[]=$cell;
	}
}

?>