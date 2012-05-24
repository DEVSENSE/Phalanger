<?php

class CategoryDataList extends TTemplateControl
{
	public function setProjectID($value)
	{
		$this->setViewState('ProjectID', $value, '');
	}
	
	public function getProjectID()
	{
		return $this->getViewState('ProjectID', '');
	}
	
	public function getCategories()
	{
		$this->ensureChildControls();
        return $this->getRegisteredObject('categories');
	}
	
	protected function getCategoryDao()
	{
		return $this->Application->Modules['daos']->getDao('CategoryDao');
	}
		
	public function showCategories()
	{
		$categoryDao = $this->getCategoryDao();
		$list = $categoryDao->getCategoriesByProjectID($this->getProjectID());
		$this->categories->DataSource = $list;
		$this->categories->dataBind();
	}
	
	public function deleteCategoryItem($sender, $param)
	{
		$id = $this->categories->DataKeys[$param->Item->ItemIndex];
		$this->getCategoryDao()->deleteCategory($id);
		$this->refreshCategoryList($sender, $param);
	}
	
	public function editCategoryItem($sender, $param)
	{
		$this->categories->EditItemIndex=$param->Item->ItemIndex;
		$this->showCategories();
	}
	
	public function refreshCategoryList($sender, $param)
	{
		$this->categories->EditItemIndex=-1;
		$this->showCategories();
	}
	
	public function updateCategoryItem($sender, $param)
	{		
		if(!$this->Page->IsValid)
			return;
			
		$item = $param->Item;
		
		$id = $this->categories->DataKeys[$param->Item->ItemIndex];
		$category = new CategoryRecord;
		$category->ID = $id;
		$category->Name = $item->name->Text;
		$category->Abbreviation = $item->abbrev->Text;
		$category->EstimateDuration = floatval($item->duration->Text);
		$category->ProjectID = $this->getProjectID();
		
		$this->getCategoryDao()->updateCategory($category);
		
		$this->refreshCategoryList($sender, $param);
	}
	
	public function addCategory_clicked($sender, $param)
	{
		if(!$this->Page->IsValid)
			return;
		
		$newCategory = new CategoryRecord;
		$newCategory->Name = $this->categoryName->Text;
		$newCategory->Abbreviation = $this->abbrev->Text;
		$newCategory->EstimateDuration = floatval($this->duration->Text);
		$newCategory->ProjectID = $this->getProjectID();
			
		$this->getCategoryDao()->addNewCategory($newCategory);
		
		$this->categoryName->Text = '';
		$this->abbrev->Text = '';
		$this->duration->Text = '';
		
		$this->showCategories();
	}	
}

?>