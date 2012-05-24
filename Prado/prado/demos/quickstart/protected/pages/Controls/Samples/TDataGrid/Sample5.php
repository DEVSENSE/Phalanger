<?php

Prado::using('Application.pages.Controls.Samples.TDataGrid.Sample1');

class Sample5 extends Sample1
{
	public function changePage($sender,$param)
	{
		$this->DataGrid->CurrentPageIndex=$param->NewPageIndex;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}

	public function pagerCreated($sender,$param)
	{
		$param->Pager->Controls->insertAt(0,'Page: ');
	}

	public function changePagerPosition($sender,$param)
	{
		$top=$sender->Items[0]->Selected;
		$bottom=$sender->Items[1]->Selected;
		if($top && $bottom)
			$position='TopAndBottom';
		else if($top)
			$position='Top';
		else if($bottom)
			$position='Bottom';
		else
			$position='';
		if($position==='')
			$this->DataGrid->PagerStyle->Visible=false;
		else
		{
			$this->DataGrid->PagerStyle->Position=$position;
			$this->DataGrid->PagerStyle->Visible=true;
		}
	}

	public function useNumericPager($sender,$param)
	{
		$this->DataGrid->PagerStyle->Mode='Numeric';
		$this->DataGrid->PagerStyle->NextPageText=$this->NextPageText->Text;
		$this->DataGrid->PagerStyle->PrevPageText=$this->PrevPageText->Text;
		$this->DataGrid->PagerStyle->PageButtonCount=$this->PageButtonCount->Text;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}

	public function useNextPrevPager($sender,$param)
	{
		$this->DataGrid->PagerStyle->Mode='NextPrev';
		$this->DataGrid->PagerStyle->NextPageText=$this->NextPageText->Text;
		$this->DataGrid->PagerStyle->PrevPageText=$this->PrevPageText->Text;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}

	public function changePageSize($sender,$param)
	{
		$this->DataGrid->PageSize=TPropertyValue::ensureInteger($this->PageSize->Text);
		$this->DataGrid->CurrentPageIndex=0;
		$this->DataGrid->DataSource=$this->Data;
		$this->DataGrid->dataBind();
	}
}

?>