<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		if($param instanceof TCommandEventParameter)
		{
			$this->Result2->Text="Command name: $param->CommandName, Command parameter: $param->CommandParameter";
		}
		else
		{
			$this->Result->Text="You clicked at ($param->X,$param->Y)";
		}
	}
}

?>