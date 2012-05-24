<?php
/**
 * EditCategory class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: EditCategory.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * EditCategory class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class EditCategory extends BlogPage
{
	private $_category;

	public function onInit($param)
	{
		parent::onInit($param);
		$id=TPropertyValue::ensureInteger($this->Request['id']);
		$this->_category=$this->DataAccess->queryCategoryByID($id);
		if($this->_category===null)
			throw new BlogException(500,'category_id_invalid',$id);
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$this->CategoryName->Text=$this->_category->Name;
			$this->CategoryDescription->Text=$this->_category->Description;
		}
	}

	public function saveButtonClicked($sender,$param)
	{
		if($this->IsValid)
		{
			$this->_category->Name=$this->CategoryName->Text;
			$this->_category->Description=$this->CategoryDescription->Text;
			$this->DataAccess->updateCategory($this->_category);
			$this->gotoPage('Posts.ListPost',array('cat'=>$this->_category->ID));
		}
	}

	public function checkCategoryName($sender,$param)
	{
		$name=$this->CategoryName->Text;
		$param->IsValid=$this->DataAccess->queryCategoryByName($name)===null;
	}
}

?>