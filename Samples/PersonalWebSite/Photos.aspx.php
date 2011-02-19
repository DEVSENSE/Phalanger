<?
	import namespace System;
	import namespace System:::Data;
	import namespace System:::Configuration;
	import namespace System:::Web;
	import namespace System:::Web:::Security;
	import namespace System:::Web:::UI;
	import namespace System:::Web:::UI:::WebControls;
	import namespace System:::Web:::UI:::WebControls:::WebParts;
	import namespace System:::Web:::UI:::HtmlControls;

	partial class Photos_aspx extends System:::Web:::UI:::Page {

		protected function DataList1_ItemDataBound($sender, $e) {
			if ($e->Item->ItemType == ListItemType::Footer) {
				if ($this->DataList1->Items->Count == 0) $this->Panel1->Visible = true;
			}
		}

	}
?>
