<?php

class InfoBox extends TControl
{
	public function render($writer)
	{
		$writer->write("<div class=\"info\"><b class=\"tip\">Info:</b>\n");
		$body=$this->renderChildren($writer);
		$writer->write("</div>");
	}
}

?>