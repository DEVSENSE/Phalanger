<?php

class QuickStartGlobalization extends TGlobalizationAutoDetect
{
	public function init($xml)
	{
		parent::init($xml);
		$this->Application->OnBeginRequest[] = array($this, 'beginRequest');
	}

	public function beginRequest($sender, $param)
	{
		if(null == ($culture=$this->Request['lang']))
		{
			if(null !== ($cookie=$this->Request->Cookies['lang']))
				$culture = $cookie->getValue();
		}

		if(is_string($culture))
		{
			$info = new CultureInfo();
			if($info->validCulture($culture))
			{
				$this->setCulture($culture);
				$this->Response->Cookies[] = new THttpCookie('lang',$culture);
			}
		}
	}
}

?>