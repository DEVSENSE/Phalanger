<?php

class DocLink extends THyperLink
{
	const BASE_URL='http://www.pradosoft.com/docs/manual';

	public function getClassPath()
	{
		return $this->getViewState('ClassPath','');
	}

	public function setClassPath($value)
	{
		$this->setViewState('ClassPath',$value,'');
	}

	public function onPreRender($param)
	{
		parent::onPreRender($param);
		$paths=explode('.',$this->getClassPath());
		if(count($paths)>1)
		{
			$classFile=array_pop($paths).'.html';
			$this->setNavigateUrl(self::BASE_URL . '/' . implode('.',$paths) . '/' . $classFile);
			if($this->getText() === '')
				$this->setText('API Manual');
		}
	}
}

?>