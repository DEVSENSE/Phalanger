<?php

class ProjectDetails extends TPage
{
	private $allUsers;
	
	private $currentProject;
	
	protected function getCurrentProject()
	{
		if(!$this->currentProject)
		{
			$id = intval($this->Request['ProjectID']);
			if($id > 0)
				$this->currentProject = $this->getProjectDao()->getProjectByID($id);
		}
		return $this->currentProject;
	}
	
	protected function getProjectDao()
	{
		return $this->Application->Modules['daos']->getDao('ProjectDao');
	}
	
	protected function getCategoryDao()
	{
		return $this->Application->Modules['daos']->getDao('CategoryDao');
	}
	
	public function onLoad($param)
	{
		if(!$this->IsPostBack)
		{
			$this->manager->DataSource = $this->getUsersWithRole('manager');
			$this->manager->dataBind();
			$this->members->DataSource = $this->getUsersWithRole('consultant');
			$this->members->dataBind();
			
			$project = $this->getCurrentProject();
			
			if($project !== null)
			{
				$this->projectName->Text = $project->Name;
				$this->completionDate->TimeStamp = $project->CompletionDate;
				$this->description->Text = $project->Description;
				$this->estimateHours->Text = $project->EstimateDuration;
				$this->manager->SelectedValue = $project->ManagerUserName;			
				
				$this->selectProjectMembers($project->ID);
				
				$this->projectCategoryColumn->Visible = true;
				$this->categories->ProjectID = $project->ID;
				$this->categories->showCategories();
				
				$this->deleteButton->Visible = true;
				
				$this->projectList->DataSource = $this->getProjects();
				$this->projectList->dataBind();
				
			}
			else
			{
				$this->projectCategoryColumn->Visible = false;
				$this->deleteButton->Visible = false;
			}
			
		}
	}
	
	protected function getProjects()
	{
		$projects = array();
		foreach($this->getProjectDao()->getAllProjects() as $project)
		{
			if($project->Name != $this->currentProject->Name)
				$projects[$project->ID] = $project->Name;
		}
		return $projects;
	}
	
	protected function selectProjectMembers($projectID)
	{
		$members = $this->getProjectDao()->getProjectMembers($projectID);
		$this->members->SelectedValues = $members;
	}
	
	protected function getUsersWithRole($role)
	{
		if(is_null($this->allUsers))
		{
			$dao = $this->Application->Modules['daos']->getDao('UserDao');
			$this->allUsers = $dao->getAllUsers();		
		}
		$users = array();
		foreach($this->allUsers as $user)
		{
			if($user->isInRole($role))
				$users[$user->Name] = $user->Name;
		}
		return $users;
	}
	
	public function onPreRender($param)
	{
		$ids = array();
		foreach($this->members->Items as $item)
		{
			if($item->Selected)
				$ids[] = $item->Value;	
		}
		$this->setViewState('ActiveConsultants', $ids);
	}
	
	public function saveButton_clicked($sender, $param)
	{
		if(!$this->Page->IsValid)
			return;
			
		$newProject = new ProjectRecord;
		
		$projectDao = $this->getProjectDao();
		
		if($project = $this->getCurrentProject())
			$newProject = $projectDao->getProjectByID($project->ID);
		else
			$newProject->CreatorUserName = $this->User->Name;
			
		$newProject->Name = $this->projectName->Text;
		$newProject->CompletionDate = $this->completionDate->TimeStamp;
		$newProject->Description = $this->description->Text;
		$newProject->EstimateDuration = floatval($this->estimateHours->Text);
		$newProject->ManagerUserName = $this->manager->SelectedValue;
		
		if($this->currentProject)
			$projectDao->updateProject($newProject);
		else
			$projectDao->addNewProject($newProject);
		
		$this->updateProjectMembers($newProject->ID);
		
		$url = $this->Service->constructUrl('TimeTracker.ProjectDetails', 
					array('ProjectID'=> $newProject->ID));
		
		$this->Response->redirect($url);
	}
	
	protected function updateProjectMembers($projectID)
	{
		$active = $this->getViewState('ActiveConsultants');
		$projectDao = $this->getProjectDao();
		foreach($this->members->Items as $item)
		{
			if($item->Selected)
			{
				if(!in_array($item->Value, $active))
					$projectDao->addUserToProject($projectID, $item->Value);
			}
			else
			{
				if(in_array($item->Value, $active))
					$projectDao->removeUserFromProject($projectID, $item->Value);
			}
		}
	}
	
	public function deleteButton_clicked($sender, $param)
	{
		if($project = $this->getCurrentProject())
		{
			$this->getProjectDao()->deleteProject($project->ID);
			$url = $this->Service->constructUrl('TimeTracker.ProjectList');
			$this->Response->redirect($url);
		}
	}
	
	public function copyButton_clicked($sender, $param)
	{
		$project = $this->projectList->SelectedValue;
		$categoryDao = $this->getCategoryDao();
		$categories = $categoryDao->getCategoriesByProjectID($project);
		$currentProject = $this->getCurrentProject();
		foreach($categories as $cat)
		{
			$cat->ProjectID = $currentProject->ID;
			$categoryDao->addNewCategory($cat);
		}
		$this->categories->showCategories();
	}
}

?>