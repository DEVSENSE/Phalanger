<?php

class Home extends TPage
{
	public function buttonClicked($sender,$param)
	{
		if($param instanceof TCallbackEventParameter)
		{
			$this->Result2->Text="Callback parameter: $param->CallbackParameter";
		}
		else
		{
			$this->Result->Text="You clicked at ($param->X,$param->Y)";
		}
	}
}

?>