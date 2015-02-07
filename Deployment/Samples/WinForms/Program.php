<?

use System\Windows\Forms;
use WinForms\Form1;

class Program
{
	static function Main()
	{
		Forms\Application::EnableVisualStyles();
		Forms\Application::Run(new Form1());
	}
}
?>