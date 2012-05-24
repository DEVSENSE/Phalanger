<?php

require_once(dirname(__FILE__).'/BaseTestCase.php');

class ProjectDaoTestCase extends BaseTestCase
{
	protected $projectDao;

	function setup()
	{
		parent::setup();
		$app = Prado::getApplication();
		$this->projectDao = $app->getModule('daos')->getDao('ProjectDao');
		$this->flushDatabase();
	}


	function createNewProject()
	{
		$project = new ProjectRecord;
		$project->CreatorUserName = "admin";
		$project->DateCreated = time();
		$project->CompletionDate = strtotime('+1 month');
		$project->Description = 'Test project 1';
		$project->EstimateDuration = 100.5;
		$project->ManagerUserName = 'manager';
		$project->Name = 'Project 1';

		return $project;
	}

	function createNewProject2()
	{
		$project = new ProjectRecord;
		$project->CreatorUserName = "manager";
		$project->DateCreated = time();
		$project->CompletionDate = strtotime('+1 week');
		$project->Description = 'Test project 2';
		$project->EstimateDuration = 30.5;
		$project->ManagerUserName = 'manager';
		$project->Name = 'Project 2';

		return $project;
	}

	function createNewProject3()
	{
		$project = new ProjectRecord;
		$project->CreatorUserName = "manager";
		$project->DateCreated = time();
		$project->CompletionDate = strtotime('+1 day');
		$project->Description = 'Test project 3';
		$project->EstimateDuration = 5.0;
		$project->ManagerUserName = 'admin';
		$project->Name = 'Project 3';

		return $project;
	}

	function add3Projects()
	{
		$project1 = $this->createNewProject();
		$project2 = $this->createNewProject2();
		$project3 = $this->createNewProject3();

		$this->projectDao->addNewProject($project1);
		$this->projectDao->addNewProject($project2);
		$this->projectDao->addNewProject($project3);
		return array($project1,$project2,$project3);
	}

	function testCreateNewProject()
	{
		$newProject = $this->createNewProject();
		$this->projectDao->addNewProject($newProject);

		$check = $this->projectDao->getProjectByID(1);
		$this->assertEqual($newProject, $check);
	}

	function testDeleteProject()
	{
		$newProject = $this->createNewProject();
		$this->projectDao->addNewProject($newProject);

		$check = $this->projectDao->getProjectByID(1);
		$this->assertEqual($newProject, $check);

		$this->projectDao->deleteProject(1);
		$verify = $this->projectDao->getProjectByID(1);
		$this->assertNull($verify);
	}

	function testAddUserToProject()
	{
		$project = $this->createNewProject();
		$this->projectDao->addNewProject($project);

		$this->projectDao->addUserToProject($project->ID, 'admin');
		$this->projectDao->addUserToProject($project->ID, 'manager');

		$members = $this->projectDao->getProjectMembers($project->ID);

		$this->assertEqual(count($members), 2);
		$this->assertEqual($members[0], 'admin');
		$this->assertEqual($members[1], 'manager');
	}

	function testAddNullUserToProject()
	{
		$project = $this->createNewProject();
		$this->projectDao->addNewProject($project);
		try
		{
			$this->projectDao->addUserToProject($project->ID, 'asd');
			$this->fail();
		}
		catch(TDbException $e)
		{
			$this->pass();
		}
	}

	function testGetAllProjects()
	{
		$added = $this->add3Projects();

		$projects = $this->projectDao->getAllProjects();

		$this->assertEqual(count($projects),3);
		$this->assertEqual($added[0],$projects[0]);
		$this->assertEqual($added[1],$projects[1]);
		$this->assertEqual($added[2],$projects[2]);
	}

	function testGetProjectsByManagerName()
	{
		$added = $this->add3Projects();

		$projects = $this->projectDao->getProjectsByManagerName('manager');

		$this->assertEqual(count($projects),2);
		$this->assertEqual($added[0],$projects[0]);
		$this->assertEqual($added[1],$projects[1]);
	}

	function testGetProjectsByUserName()
	{
		$added = $this->add3Projects();

		$username = 'consultant';

		$this->projectDao->addUserToProject(1, $username);
		$this->projectDao->addUserToProject(3, $username);

		$projects = $this->projectDao->getProjectsByUserName($username);

		$this->assertEqual(count($projects),2);
		$this->assertEqual($added[0],$projects[0]);
		$this->assertEqual($added[2],$projects[1]);
	}

	function testRemoveUserFromProject()
	{
		$added = $this->add3Projects();
		$this->projectDao->addUserToProject(1, 'admin');
		$this->projectDao->addUserToProject(1, 'manager');
		$this->projectDao->addUserToProject(1, 'consultant');

		$members = $this->projectDao->getProjectMembers(1);

		$this->assertEqual(count($members), 3);
		$this->assertEqual($members[0], 'admin');
		$this->assertEqual($members[2], 'manager');
		$this->assertEqual($members[1], 'consultant');

		$this->projectDao->removeUserFromProject(1,'admin');

		$list = $this->projectDao->getProjectMembers(1);

		$this->assertEqual(count($list), 2);
		$this->assertEqual($list[1], 'manager');
		$this->assertEqual($list[0], 'consultant');
	}

	function testUpdateProject()
	{
		$project = $this->createNewProject();
		$this->projectDao->addNewProject($project);

		$project->Description = "Project Testing 123";

		$this->projectDao->updateProject($project);

		$check = $this->projectDao->getProjectByID(1);
		$this->assertEqual($check, $project);
	}
}

?>