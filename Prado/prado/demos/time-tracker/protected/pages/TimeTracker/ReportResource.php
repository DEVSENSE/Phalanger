<?php

class ReportResource extends TPage
{
	protected function getProjects()
	{
		$projectDao = $this->Application->Modules['daos']->getDao('ProjectDao');
		$projects = array();
		foreach($projectDao->getAllProjects() as $project)
				$projects[$project->ID] = $project->Name;
		return $projects;
	}
	
	protected function getUsers()
	{
		$dao = $this->Application->Modules['daos']->getDao('UserDao');
		$users = array();
		foreach($dao->getAllUsers() as $user)
		{
			$users[$user->Name] = $user->Name;
		}
		return $users;
	}
	
	public function onLoad($param)
	{
		if(!$this->IsPostBack)
		{
			$this->projectList->DataSource = $this->getProjects();
			$this->resourceList->DataSource = $this->getUsers();
			$this->dataBind();	
		}
	}	
	
	public function generateReport_Clicked($sender, $param)
	{
		if(count($this->projectList->SelectedValues) > 0
			&& count($this->resourceList->SelectedValues) >0)
		{
			$this->showReport();
		}
	}

	protected function showReport()
	{
		$this->views->ActiveViewIndex = 1;
		$reportDao = $this->Application->Modules['daos']->getDao('ReportDao');
		$projects = $this->projectList->SelectedValues;
		$users = $this->resourceList->SelectedValues;
		$start = $this->dateFrom->TimeStamp;
		$end = $this->dateTo->TimeStamp;
		
		$report = $reportDao->getUserProjectTimeReports($users, $projects, $start, $end);

		$this->resource_report->DataSource = $report;
		$this->resource_report->dataBind();		
	}
	
	public function resource_report_itemCreated($sender, $param)
	{
		$item = $param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
		{
			if(count($item->DataItem->Projects) > 0 &&
				$item->DataItem->Projects[0]->ProjectName !== null)
			$item->time_entries->DataSource = $item->DataItem->Projects;
		}
	}
}

?>