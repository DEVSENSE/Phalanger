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

	partial class Admin_Details_aspx extends System:::Web:::UI:::Page {

		function __construct() {
			$this->Load->Add(new EventHandler(array($this, "Page_Load")));
		}

		function Page_Load($sender, $e) {
			$this->MaintainScrollPositionOnPostBack = true;
			if ($this->IsPostBack) return;
			
			$page = Convert::ToInt32($this->Request->QueryString->get_Item("Page"));
			if ($page >= 0) $this->FormView1->PageIndex = $page;
		}

	}
?>
