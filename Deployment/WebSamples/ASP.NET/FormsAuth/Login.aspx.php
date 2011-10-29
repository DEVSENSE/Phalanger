<?
	use System\Web\Security\FormsAuthentication;
	
	partial class Login extends \System\Web\UI\Page
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
