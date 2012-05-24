<?php

class ReportProject extends TPage
{
	protected function getProjects()
	{
		$projectDao = $this->Application->Modules['daos']->getDao('ProjectDao');
		$projects = array();
		foreach($projectDao->getAllProjects() as $project)
				$projects[$project->ID] = $project->Name;
		return $projects;
	}
	
	public function onLoad($param)
	{
		if(!$this->IsPostBack)
		{
			$this->projectList->DataSource = $this->getProjects();
			$this->dataBind();	
		}
	}
	
	public function generateReport_Clicked($sender, $param)
	{
		if(count($this->projectList->SelectedValues) > 0)
			$this->showReport();
	}
	
	protected function showReport()
	{
		$reportDao = $this->Application->Modules['daos']->getDao('ReportDao');
		$reports = $reportDao->getTimeReportsByProjectIDs($this->projectList->SelectedValues);
		$this->views->ActiveViewIndex = 1;
		$this->projects->DataSource = $reports;
		$this->projects->dataBind();		
	}
	
	public function project_itemCreated($sender, $param)
	{
		$item = $param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
			$item->category->DataSource = $item->DataItem->Categories;
	}
	
	public function category_itemCreated($sender, $param)
	{
		$item = $param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
			$item->members->DataSource = $item->DataItem->members;
	}
}

?>