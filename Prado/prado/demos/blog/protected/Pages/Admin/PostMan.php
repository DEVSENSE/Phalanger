<?php
/**
 * PostMan class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: PostMan.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * PostMan class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class PostMan extends BlogPage
{
	protected function bindData()
	{
		$offset=$this->PostGrid->CurrentPageIndex*$this->PostGrid->PageSize;
		$limit=$this->PostGrid->PageSize;
		$this->PostGrid->DataSource=$this->DataAccess->queryPosts('','','ORDER BY a.status DESC, modify_time DESC',"LIMIT $offset,$limit");
		$this->PostGrid->VirtualItemCount=$this->DataAccess->queryPostCount('','');
		$this->PostGrid->dataBind();
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		if(!$this->IsPostBack)
			$this->bindData();
	}

	public function changePage($sender,$param)
	{
		$this->PostGrid->CurrentPageIndex=$param->NewPageIndex;
		$this->bindData();
	}

	public function pagerCreated($sender,$param)
	{
		$param->Pager->Controls->insertAt(0,'Page: ');
	}

	public function editItem($sender,$param)
	{
		$this->PostGrid->EditItemIndex=$param->Item->ItemIndex;
		$this->bindData();
	}

	public function saveItem($sender,$param)
	{
		$item=$param->Item;
		$postID=$this->PostGrid->DataKeys[$item->ItemIndex];
		$postRecord=$this->DataAccess->queryPostByID($postID);
		$postRecord->Status=TPropertyValue::ensureInteger($item->Cells[2]->PostStatus->SelectedValue);
		$this->DataAccess->updatePost($postRecord);
		$this->PostGrid->EditItemIndex=-1;
		$this->bindData();
	}

	public function cancelItem($sender,$param)
	{
		$this->PostGrid->EditItemIndex=-1;
		$this->bindData();
	}
}

?>