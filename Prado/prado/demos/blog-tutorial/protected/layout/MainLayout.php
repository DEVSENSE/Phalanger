<?php

class MainLayout extends TTemplateControl
{

	public function onLoad($param)
	{
		parent::onLoad($param);
		
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