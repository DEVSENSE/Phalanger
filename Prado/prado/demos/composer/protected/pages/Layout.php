<?php

class Layout extends TTemplateControl
{
	public function toggleTopicPanel($sender,$param)
	{
		$this->TopicPanel->Visible=!$this->TopicPanel->Visible;
		if($this->TopicPanel->Visible)
			$sender->Text="Hide TOC";
		else
			$sender->Text="Show TOC";
	}
}

?>