<?php

class TipBox extends TControl
{
	public function render($writer)
	{
		$writer->write("<div class=\"tip\"><b class=\"tip\">Tip:</b>\n");
		$body=$this->renderChildren($writer);
		$writer->write("</div>");
	}
}

?>