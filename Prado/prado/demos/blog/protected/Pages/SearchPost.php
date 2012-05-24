<?php

class SearchPost extends BlogPage
{
	private $_posts;

	public function onInit($param)
	{
		parent::onInit($param);
		$this->_posts=$this->DataAccess->queryPosts(
				$this->getPostFilter(),
				'',
				'ORDER BY create_time DESC',
				'LIMIT '.$this->getPageOffset().','.$this->getPageSize());
	}

	private function getPostFilter()
	{
		$filter='a.status=0';
		$keywords=explode(' ',$this->Request['keyword']);
		foreach($keywords as $keyword)
		{
			if(($keyword=$this->DataAccess->escapeString(trim($keyword)))!=='')
				$filter.=" AND (content LIKE '%$keyword%' OR title LIKE '%$keyword%')";
		}
		return $filter;
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
}

?>