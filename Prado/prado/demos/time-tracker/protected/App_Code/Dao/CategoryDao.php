<?php

class CategoryDao extends BaseDao
{
	function addNewCategory($category)
	{
		$sqlmap = $this->getSqlMap();
		$exists = $this->getCategoryByNameInProject(
			$category->Name, $category->ProjectID);
		if(!$exists)
			$sqlmap->insert('AddNewCategory', $category);
	}

	function getCategoryByID($categoryID)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForObject('GetCategoryByID', $categoryID);
	}

	function getAllCategories()
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForList('GetAllCategories');
	}

	function deleteCategory($categoryID)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->delete('DeleteCategory', $categoryID);
	}

	function getCategoriesByProjectID($projectID)
	{
		$sqlmap = $this->getSqlMap();
		return $sqlmap->queryForList('GetCategoriesByProjectID', $projectID);
	}

	function getCategoryByNameInProject($name, $projectID)
	{
		$sqlmap = $this->getSqlMap();
		$param['project'] = $projectID;
		$param['category'] = $name;
		return $sqlmap->queryForObject('GetCategoryByNameInProject', $param);
	}

	function updateCategory($category)
	{
		$sqlmap = $this->getSqlMap();
		$sqlmap->update('UpdateCategory', $category);
	}
}

?>