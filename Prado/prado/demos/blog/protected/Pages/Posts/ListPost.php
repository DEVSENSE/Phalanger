<?php
/**
 * ListPost class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: ListPost.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * ListPost class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class ListPost extends BlogPage
{
	private $_posts;
	private $_category;

	public function onInit($param)
	{
		parent::onInit($param);
		$this->_posts=$this->DataAccess->queryPosts(
				$this->getPostFilter(),
				$this->getCategoryFilter(),
				'ORDER BY a.status DESC, create_time DESC',
				'LIMIT '.$this->getPageOffset().','.$this->getPageSize());
		if($this->Request['cat']!==null)
		{
			$catID=TPropertyValue::ensureInteger($this->Request['cat']);
			$this->_category=$this->DataAccess->queryCategoryByID($catID);
			$this->CategoryPanel->Visible=true;
		}
		$this->Title=$this->Application->Parameters['SiteTitle'];
	}

	private function getPageOffset()
	{
		if(($offset=TPropertyValue::ensureInteger($this->Request['offset']))<=0)
			$offset=0;
		return $offset;
	}

	private function getPageSize()
	{
		if(($limit=TPropertyValue::ensureInteger($this->Request['limit']))<=0)
			$limit=TPropertyValue::ensureInteger($this->Application->Parameters['PostPerPage']);
		return $limit;
	}

	private function getTimeFilter()
	{
		if(($time=TPropertyValue::ensureInteger($this->Request['time']))>0)
		{
			$year=(integer)($time/100);
			$month=$time%100;
			$startTime=mktime(0,0,0,$month,1,$year);
			if(++$month>12)
			{
				$month=1;
				$year++;
			}
			$endTime=mktime(0,0,0,$month,1,$year);
			return "create_time>=$startTime AND create_time<$endTime";
		}
		else
			return '';
	}

	private function getPostFilter()
	{
		$filter='(a.status=0 OR a.status=3)';
		if(($timeFilter=$this->getTimeFilter())!=='')
			return "$filter AND $timeFilter";
		else
			return $filter;
	}

	private function getCategoryFilter()
	{
		if(($catID=$this->Request['cat'])!==null)
		{
			$catID=TPropertyValue::ensureInteger($catID);
			return "category_id=$catID";
		}
		else
			return '';
	}

	private function formUrl($newOffset)
	{
		$gets=array();
		$gets['offset']=$newOffset;
		if($this->Request['limit']!==null)
			$gets['limit']=$this->Request['limit'];
		if($this->Request['time']!==null)
			$gets['time']=$this->Request['time'];
		if($this->Request['cat']!==null)
			$gets['cat']=$this->Request['cat'];
		return $this->Service->constructUrl('Posts.ListPost',$gets);
	}

	public function getCategory()
	{
		return $this->_category;
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		$this->PostList->DataSource=$this->_posts;
		$this->PostList->dataBind();
		if($this->getPageOffset()>0)
		{
			if(($offset=$this->getPageOffset()-$this->getPageSize())<0)
				$offset=0;
			$this->PrevPage->NavigateUrl=$this->formUrl($offset);
			$this->PrevPage->Visible=true;
		}
		if(count($this->_posts)===$this->getPageSize())
		{
			$this->NextPage->NavigateUrl=$this->formUrl($this->getPageOffset()+$this->getPageSize());
			$this->NextPage->Visible=true;
		}
	}

	public function deleteButtonClicked($sender,$param)
	{
		if($this->User->IsAdmin)
		{
			$this->DataAccess->deleteCategory($this->Category->ID);
			$this->gotoDefaultPage();
		}
	}
}

?>