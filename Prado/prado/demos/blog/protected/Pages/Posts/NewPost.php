<?php
/**
 * NewPost class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: NewPost.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * NewPost class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class NewPost extends BlogPage
{
	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
		{
			$this->Categories->DataSource=$this->DataAccess->queryCategories();
			$this->Categories->dataBind();
		}
	}

	public function saveButtonClicked($sender,$param)
	{
		if($this->IsValid)
		{
			$postRecord=new PostRecord;
			$postRecord->Title=$this->Title->SafeText;
			$postRecord->Content=$this->Content->SafeText;
			if($this->DraftMode->Checked)
				$postRecord->Status=PostRecord::STATUS_DRAFT;
			else if(!$this->User->IsAdmin && TPropertyValue::ensureBoolean($this->Application->Parameters['PostApproval']))
				$postRecord->Status=PostRecord::STATUS_PENDING;
			else
				$postRecord->Status=PostRecord::STATUS_PUBLISHED;
			$postRecord->CreateTime=time();
			$postRecord->ModifyTime=$postRecord->CreateTime;
			$postRecord->AuthorID=$this->User->ID;
			$cats=array();
			foreach($this->Categories->SelectedValues as $value)
				$cats[]=TPropertyValue::ensureInteger($value);
			$this->DataAccess->insertPost($postRecord,$cats);
			$this->gotoPage('Posts.ViewPost',array('id'=>$postRecord->ID));
		}
	}
}

?>