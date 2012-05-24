<?php

class RegionDisplay extends TRepeaterItemRenderer
{
	/**
	 * This method is invoked when the data is being bound
	 * to the parent repeater.
	 * At this time, the <b>Data</b> is available which
	 * refers to the data row associated with the parent repeater item.
	 */
	public function onDataBinding($param)
	{
		parent::onDataBinding($param);
		$this->Repeater->DataSource=$this->Data['detail'];
		$this->Repeater->dataBind();
	}

	public function itemCreated($sender,$param)
	{
		static $itemIndex=0;
		$item=$param->Item;
		if($item->ItemType==='Item' || $item->ItemType==='AlternatingItem')
			$item->Row->BackColor=$itemIndex%2 ? "#BFCFFF" : "#E6ECFF";
		$itemIndex++;
	}
}

?>