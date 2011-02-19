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

	partial class Default_aspx extends System:::Web:::UI:::Page {

		public function Randomize($sender, $e) {
			$r = new Random;
			$this->FormView1->PageIndex = $r->Next($this->FormView1->PageCount);
		}
	}
?>
