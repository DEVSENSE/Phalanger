<?php

class RequiresVersion extends TTemplateControl
{
	public function setVersion($value)
	{
		$this->setViewState('Version',$value);
	}

	public function getVersion()
	{
		return $this->getViewState('Version');
	}
}

?>