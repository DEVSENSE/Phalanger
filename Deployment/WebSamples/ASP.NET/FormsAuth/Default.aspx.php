<?
	import namespace System;
	import namespace System:::Data;
	import namespace System:::Configuration;
	import namespace System:::Collections;
	import namespace System:::Web;
	import namespace System:::Web:::Security;
	import namespace System:::Web:::UI;
	import namespace System:::Web:::UI:::WebControls;
	import namespace System:::Web:::UI:::WebControls:::WebParts;
	import namespace System:::Web:::UI:::HtmlControls;

	partial class _Default extends System:::Web:::UI:::Page
	{
		protected function ButtonLogout_Click($sender, $e)
		{
			FormsAuthentication::SignOut();
			FormsAuthentication::RedirectToLoginPage();
		}		
	}
?>
