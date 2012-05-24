<?php

class NoteBox extends TControl
{
	public function render($writer)
	{
		$writer->write("<div class=\"note\"><b class=\"tip\">Note:</b>\n");
		$body=$this->renderChildren($writer);
		$writer->write("</div>");
	}
}

?>