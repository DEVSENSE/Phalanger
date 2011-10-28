<?
	namespace Phalanger
	{
		class Demo
		{
			static function Main()
			{
				echo "\n";
				echo "Welcome to Phalanger 2.0 Milestone 3 Demo\n";
				echo "=========================================\n";
				echo "\n";
				
				while (true)
				{
					echo "1. .NET Interoperability\n";
					echo "2. Generics support\n";
					echo "3. LINQ support\n";
					echo "\n";
					echo "0. Exit\n";
					echo "\n";
					echo "Enter your choice: ";
					
					$line = \System\Console::ReadLine();
					echo "\n";
					
					switch ($line)
					{
						case "0": exit;
						case "1": Interop::Run();  break;
						case "2": Generics::Run(); break;
						case "3": Linq::Run();     break;
					}
				}
			}
		}
	}
?>
