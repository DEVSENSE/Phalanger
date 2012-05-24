<?php

require_once(dirname(__FILE__).'/BaseTestCase.php');

class CategoryDaoTestCase extends BaseTestCase
{
	protected $categoryDao;
	protected $projectDao;
	
	function setup()
	{
		parent::setup();
		$app = Prado::getApplication();
		$this->categoryDao = $app->getModule('daos')->getDao('CategoryDao');
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

	function create3Categories()
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
		
		return array($category1, $category2, $category3);
	}

	function testCreateNewCategory()
	{		
		$project = $this->createNewProject();
		$this->projectDao->addNewProject($project);
		
		$category = $this->createNewCategory();
		$category->ProjectID = $project->ID;
		
		$this->categoryDao->addNewCategory($category);
		
		$check = $this->categoryDao->getCategoryByID(1);

		$this->assertEqual($category, $check);
	}
	
	function testCreateDuplicateCategory()
	{		
		$project = $this->createNewProject();
		$this->projectDao->addNewProject($project);
		
		$category = $this->createNewCategory();
		$category->ProjectID = $project->ID;

		$this->categoryDao->addNewCategory($category);
		
		try
		{
			$this->categoryDao->addNewCategory($category);
			$this->pass();
		}
		catch(TSqlMapQueryExecutionException $e)
		{
			$this->fail();
		}
		$check = $this->categoryDao->getCategoryByID(1);
		$this->assertEqual($category, $check);
	}
	
	function testGetAllCategories()
	{
		$added = $this->create3Categories();
				
		$list = $this->categoryDao->getAllCategories();
		$this->assertEqual(count($list), 3);
		$this->assertEqual($added[0], $list[0]);
		$this->assertEqual($added[1], $list[1]);
		$this->assertEqual($added[2], $list[2]);
	}

	function testDeleteCategory()
	{
		$added = $this->create3Categories();
		
		$this->categoryDao->deleteCategory(1);
		
		$list = $this->categoryDao->getAllCategories();
		
		$this->assertEqual(count($list), 2);
		$this->assertEqual($added[1], $list[0]);
		$this->assertEqual($added[2], $list[1]);		
	}

	function testCategoriesInProject()
	{
		$added = $this->create3Categories();
		
		$list = $this->categoryDao->getCategoriesByProjectID(1);
	
		$this->assertEqual(count($list), 2);
		$this->assertEqual($added[0], $list[0]);
		$this->assertEqual($added[2], $list[1]);			
	}
	
	function testGetCategoryByCategoryNameandProjectId()
	{
		$added = $this->create3Categories();
		$cat = $this->categoryDao->getCategoryByNameInProject('Category 1', 1);
		
		$this->assertEqual($cat, $added[0]);
	}
	
	function testUpdateCategory()
	{
		$project = $this->createNewProject();
		$this->projectDao->addNewProject($project);
		
		$category = $this->createNewCategory();
		$category->ProjectID = $project->ID;

		$this->categoryDao->addNewCategory($category);
		
		$category->Name = "Test 2";
		$this->categoryDao->updateCategory($category);
		
		$check = $this->categoryDao->getCategoryByID($category->ID);
		
		$this->assertEqual($category, $check);
	}
}

?>