<?php

class Layout extends TTemplateControl
{
	public function __construct()
	{
		if(isset($this->Request['notheme']))
			$this->Service->RequestedPage->EnableTheming=false;
		parent::__construct();
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		$url=$this->Request->RequestUri;
		if(strpos($url,'?')===false)
			$url.='?notheme=true';
		else
			$url.='&amp;notheme=true';
		$this->PrinterLink->NavigateUrl=$url;

		if(isset($this->Request['notheme']))
		{
			$this->MainMenu->Visible=false;
			$this->TopicPanel->Visible=false;
		}

		$this->languages->DataSource = TPropertyValue::ensureArray($this->Application->Parameters['languages']);
		$this->languages->dataBind();
	}

	public function languageLinkCreated($sender, $param)
	{
		$item = $param->Item;
		if($item->ItemType == TListItemType::Item || $item->ItemType == TListItemType::AlternatingItem)
		{
			$params = $this->Request->toArray();
			$params['lang'] = $sender->DataKeys[$item->ItemIndex];
			unset($params[$this->Request->ServiceID]);
			$url = $this->Service->ConstructUrl($this->Service->RequestedPagePath, $params);
			$item->link->NavigateUrl = $url;
			if($this->Application->Globalization->Culture == $params['lang'])
				$item->link->CssClass="active";
		}
	}
}

?>