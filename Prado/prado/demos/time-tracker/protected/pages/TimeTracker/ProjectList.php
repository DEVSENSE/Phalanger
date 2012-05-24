<?php

class ProjectList extends TPage
{
	protected function showProjects($sort='', $order='')
	{
		$dao = $this->Application->Modules['daos']->getDao('ProjectDao');
		$this->projectList->DataSource = $dao->getAllProjects($sort, $order);
		$this->projectList->dataBind();
	}
	
	protected function getSortOrdering($sort)
	{
		$ordering = $this->getViewState('SortOrder', array());
		$order = isset($ordering[$sort]) ? $ordering[$sort] : 'DESC';
		$ordering[$sort] = $order == 'DESC' ? 'ASC' : 'DESC';
		$this->setViewState('SortOrder', $ordering);
		return $ordering[$sort];
	}
	
	protected function sortProjects($sender, $param)
	{
		$sort = $param->SortExpression;
		$this->showProjects($sort, $this->getSortOrdering($sort));
	}
		
	public function onLoad($param)
	{
		if(!$this->IsPostBack)
			$this->showProjects();
	}
}

?>