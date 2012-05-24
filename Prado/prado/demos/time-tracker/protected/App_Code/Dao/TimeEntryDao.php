<?php

class TimeEntryDao extends BaseDao
{
	public function addNewTimeEntry($entry)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->insert('AddNewTimeEntry', $entry);
	}

	public function getTimeEntryByID($entryID)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForObject('GetTimeEntryByID', $entryID);
	}

	public function deleteTimeEntry($entryID)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->delete('DeleteTimeEntry', $entryID);
	}

	public function getTimeEntriesInProject($username, $projectID)
	{
		$sqlmap = $this->getSqlMap();
		$param['username'] = $username;
		$param['project'] = $projectID;
		return $sqlmap->queryForList('GetAllTimeEntriesByProjectIdAndUser', $param);
	}

	public function updateTimeEntry($entry)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->update('UpdateTimeEntry', $entry);
	}
}

?>