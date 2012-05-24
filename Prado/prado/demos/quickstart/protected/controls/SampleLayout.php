<?php

class SampleLayout extends TTemplateControl
{
	public function __construct()
	{
		if(isset($this->Request['notheme']))
			$this->Service->RequestedPage->EnableTheming=false;
		parent::__construct();
	}
}

?>