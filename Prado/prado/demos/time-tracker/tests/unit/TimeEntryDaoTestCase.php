<?php

require_once(dirname(__FILE__).'/BaseTestCase.php');

class TimeEntryDaoTestCase extends BaseTestCase
{
	protected $entryDao;
	protected $projectDao;
	protected $userDao;
	protected $categoryDao;
	protected $reportDao;
	
	function setup()
	{
		parent::setup();
		$app = Prado::getApplication();
		$this->entryDao = $app->getModule('daos')->getDao('TimeEntryDao');
		$this->projectDao = $app->getModule('daos')->getDao('ProjectDao');
		$this->userDao = $app->getModule('daos')->getDao('UserDao');
		$this->categoryDao = $app->getModule('daos')->getDao('CategoryDao');
		$this->reportDao = $app->getModule('daos')->getDao('ReportDao');
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
	
	function createNewCategory()
	{
		$category = new CategoryRecord;
		$category->Name = 'Category 1';
		$category->EstimateDuration = 5.5;
		$category->Abbreviation = 'CAT 1';
		
		return $category;		
	}
	
	function createNewCategory2()
	{
		$category = new CategoryRecord;
		$category->Name = 'Category 2';
		$category->EstimateDuration = 1.5;
		$category->Abbreviation = 'CAT2';
		
		return $category;		
	}

	function createNewCategory3()
	{
		$category = new CategoryRecord;
		$category->Name = 'Category 3';
		$category->EstimateDuration = 2.5;
		$category->Abbreviation = 'CAT3';
		
		return $category;		
	}

	function createProjectsAndCategories()
	{
		$project1 = $this->createNewProject();
		$this->projectDao->addNewProject($project1);

		$project2 = $this->createNewProject2();
		$this->projectDao->addNewProject($project2);
		
		$category1 = $this->createNewCategory();
		$category1->ProjectID = $project1->ID;
		
		$category2 = $this->createNewCategory2();
		$category2->ProjectID = $project2->ID;

		$category3 = $this->createNewCategory3();
		$category3->ProjectID = $project1->ID;

		$this->categoryDao->addNewCategory($category1);
		$this->categoryDao->addNewCategory($category2);
		$this->categoryDao->addNewCategory($category3);
		
		return array($project1, $project2, $category1, $category2, $category3);
	}
	
	function assertSameEntry($entry1, $entry2)
	{
		$this->assertEqual($entry1->CreatorUserName, $entry2->CreatorUserName);
		$this->assertEqual($entry1->Description, $entry2->Description);
		$this->assertEqual($entry1->Duration, $entry2->Duration);
		$this->assertEqual($entry1->ID, $entry2->ID);
		$this->assertEqual($entry1->ReportDate, $entry2->ReportDate);
		$this->assertEqual($entry1->Username, $entry2->Username);
	}
	
	
	function createTimeEntry1()
	{
		$added = $this->createProjectsAndCategories();		
		
		$entry = new TimeEntryRecord;
		$entry->CreatorUserName = "admin";
		$entry->Category = $added[2];
		$entry->Description = "New work";
		$entry->Duration = 1.5;
		$entry->Project = $added[0];
		$entry->ReportDate = strtotime('-1 day');
		$entry->Username = 'consultant';
		
		return array($entry, $added);
	}

	function createTimeEntries2()
	{
		$added = $this->createProjectsAndCategories();		
		
		$entry = new TimeEntryRecord;
		$entry->CreatorUserName = "admin";
		$entry->Category = $added[2];
		$entry->Description = "New work";
		$entry->Duration = 1.2;
		$entry->Project = $added[0];
		$entry->ReportDate = strtotime('-10 day');
		$entry->Username = 'consultant';
		
		$entry2 = new TimeEntryRecord;
		$entry2->CreatorUserName = "admin";
		$entry2->Category = $added[4];
		$entry2->Description = "New work 2";
		$entry2->Duration = 5.5;
		$entry2->Project = $added[0];
		$entry2->ReportDate = strtotime('-4 day');
		$entry2->Username = 'consultant';

		return array($entry, $entry2, $added);
	}
	
	function testCreateNewTimeEntry()
	{
		$added = $this->createTimeEntry1();
		$entry = $added[0];
		$this->entryDao->addNewTimeEntry($entry);
		
		$check = $this->entryDao->getTimeEntryByID(1);
		
		$this->assertSameEntry($entry, $check);
	}
	
	function testDeleteTimeEntry()
	{
		$this->testCreateNewTimeEntry();
		$this->entryDao->deleteTimeEntry(1);
		
		$check = $this->entryDao->getTimeEntryByID(1);
		$this->assertNull($check);
	}	

	function testGetEntriesInProject()
	{
		$added = $this->createTimeEntries2();
		$this->entryDao->addNewTimeEntry($added[0]);
		$this->entryDao->addNewTimeEntry($added[1]);
		
		$list = $this->entryDao->getTimeEntriesInProject('consultant', 1);
		
		$this->assertEqual(count($list), 2);
		
		$this->assertSameEntry($list[0], $added[0]);
		$this->assertSameEntry($list[1], $added[1]);
	}

	function testUpdateEntry()
	{
		$added = $this->createTimeEntry1();
		$entry = $added[0];
		$this->entryDao->addNewTimeEntry($entry);
		
		$check = $this->entryDao->getTimeEntryByID(1);
		
		$this->assertSameEntry($entry, $check);
		
		$entry->Description = "asdasd";
		$entry->Duration = 200;
		
		$this->entryDao->updateTimeEntry($entry);
		
		$verify = $this->entryDao->getTimeEntryByID(1);
		
		$this->assertSameEntry($entry, $verify);
	}
}

?>