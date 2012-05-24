<?php

class RunBar extends TTemplateControl
{
	public function getPagePath()
	{
		return $this->getViewState('PagePath','');
	}

	public function setPagePath($value)
	{
		$this->setViewState('PagePath',$value,'');
	}

	public function onPreRender($param)
	{
		$pagePath=$this->getPagePath();
		$this->RunButton->NavigateUrl="?page=$pagePath";
		$this->ViewSourceButton->NavigateUrl="?page=ViewSource&path=/".strtr($pagePath,'.','/').'.page';
	}
}

?>