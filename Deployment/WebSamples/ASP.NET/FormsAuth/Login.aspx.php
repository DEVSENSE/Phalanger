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

	partial class Login extends System:::Web:::UI:::Page
	{
		protected function ButtonSubmit_Click($sender, $e)
		{
			$login = $this->TextBoxLogin->Text;
			$passw = $this->TextBoxPassword->Text;
		
			if (FormsAuthentication::Authenticate($login, $passw))
			{
				FormsAuthentication::RedirectFromLoginPage($login, false);
			}
			else
			{
				$this->CustomLoginValidator->IsValid = false;
			}
		}
	}
?>
