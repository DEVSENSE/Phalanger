<?
	use System\Web\Security\FormsAuthentication;
	
	partial class _Default extends \System\Web\UI\Page
	{
		protected function ButtonLogout_Click($sender, $e)
		{
			FormsAuthentication::SignOut();
			FormsAuthentication::RedirectToLoginPage();
		}		
	}
?>
