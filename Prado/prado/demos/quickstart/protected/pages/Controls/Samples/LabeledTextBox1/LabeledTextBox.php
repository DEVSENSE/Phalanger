<?php

class LabeledTextBox extends TTemplateControl
{
	public function getLabel()
	{
		$this->ensureChildControls();
		return $this->getRegisteredObject('Label');
	}

	public function getTextBox()
	{
		$this->ensureChildControls();
		return $this->getRegisteredObject('TextBox');
	}
}

?>