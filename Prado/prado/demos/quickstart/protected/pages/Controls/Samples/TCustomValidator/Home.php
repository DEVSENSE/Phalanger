<?php

class Home extends TPage
{
	public function serverValidate($sender,$param)
	{
		if($param->Value!=='test')
			$param->IsValid=false;
	}
}

?>