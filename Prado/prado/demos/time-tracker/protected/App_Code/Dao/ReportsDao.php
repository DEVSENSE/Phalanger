<?php

class ProjectReport extends TComponent
{
	public $ProjectName = '';
	public $EstimateHours = 0;
	public $EstimateCompletion = 0;
	public $Categories;

	public function __construct()
	{
		$this->Categories = new TList;
	}

	public function getActualHours()
	{
		$total = 0;
		foreach($this->Categories as $cat)
			$total += $cat->getActualHours();
		return $total;
	}
}

class CategoryReport extends TComponent
{
	public $CategoryName = '';
	public $EstimateHours = 0;
	public $members = array();

	public function getActualHours()
	{
		$total = 0;
		foreach($this->members as $member)
			$total += $member['hours'];
		return $total;
	}
}

class UserReport extends TComponent
{
	public $Username;
	public $Projects = array();

	public function getTotalHours()
	{
		$hours = 0;
		foreach($this->Projects as $project)
			$hours += $project->Duration;
		return $hours;
	}
}

class UserProjectReport
{
	public $ProjectName = '';
	public $CategoryName = '';
	public $Duration = 0;
	public $Description='';
	public $ReportDate=0;
}

class ReportsDao extends BaseDao
{
	public function getTimeReportsByProjectIDs($projects)
	{
		$ids = implode(',', array_map('intval', $projects));
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForList('GetTimeReportByProjectIDs', $ids);
	}

	public function getUserProjectTimeReports($users, $projects, $startDate, $endDate)
	{
		$sqlmap = $this->getSqlMap();
		$ids = implode(',', array_map('intval', $projects));
		$sqlmap->getDbConnection()->setActive(true); //db connection needs to be open for quoteString
		$usernames = implode(',', array_map(array($sqlmap->getDbConnection(), 'quoteString'), $users));

		$param['projects'] = $ids;
		$param['members'] = $usernames;
		$param['startDate'] = intval($startDate);
		$param['endDate'] = intval($endDate);

		return $sqlmap->queryForList('GetTimeReportByUsername', $param);
	}
}

?>