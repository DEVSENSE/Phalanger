<?php
/**
 * Project DAO class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: ProjectDao.php 1578 2006-12-17 22:20:50Z wei $
 * @package Demos
 */

/**
 * Project DAO class.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: ProjectDao.php 1578 2006-12-17 22:20:50Z wei $
 * @package Demos
 * @since 3.1
 */
class ProjectDao extends BaseDao
{
	public function projectNameExists($projectName)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForObject('ProjectNameExists', $projectName);
	}

	public function addNewProject($project)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->insert('CreateNewProject', $project);
	}

	public function getProjectByID($projectID)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForObject('GetProjectByID', $projectID);
	}

	public function deleteProject($projectID)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->update('DeleteProject',$projectID);
	}

	public function addUserToProject($projectID, $username)
	{
		$sqlmap = $this->getSqlMap();
		$members = $this->getProjectMembers($projectID);
		if(!in_array($username, $members))
		{
			$param['username'] = $username;
			$param['project'] = $projectID;
			$sqlmap->insert('AddUserToProject',$param);
		}
	}

	public function getProjectMembers($projectID)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForList('GetProjectMembers', $projectID);
	}

	public function getAllProjects($sort='', $order='ASC')
	{
		$sqlmap = $this->getSqlMap();
		if($sort === '')
			return $sqlmap->queryForList('GetAllProjects');
		else
		{
			$param['sort'] = $sort;
			$param['order'] = $order;
			return $sqlmap->queryForList('GetAllProjectsOrdered', $param);
		}
	}

	public function getProjectsByManagerName($manager)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForList('GetProjectsByManagerName', $manager);
	}

	public function getProjectsByUserName($username)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForList('GetProjectsByUserName', $username);
	}

	public function removeUserFromProject($projectID, $username)
	{
		$sqlmap = $this->getSqlMap();
		$param['username'] = $username;
		$param['project'] = $projectID;
		$sqlmap->delete('RemoveUserFromProject', $param);
	}

	public function updateProject($project)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->update('UpdateProject', $project);
	}
}

?>