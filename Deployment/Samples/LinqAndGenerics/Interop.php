<?
	// This script demonstrates interoperating with other .NET languages.
	// PHP is now a fully-fledged extender, producer, and consumer.

	namespace Phalanger
	{

        use System as S;

		// the class is decorated with a custom attribute
		[\System\Runtime\InteropServices\ComVisible(false)]
		class MyComparer implements \System\Collections\IEqualityComparer
		{
			// The two methods in this class implement the IEqualityComparer interface. Overloads that match
			// the interface signatures are automatically generated. If there were more overloads of e.g. the
			// Equals method in the interface, corresponding stubs would be generated automatically and they
			// would all delegate to the one PHP method below.
		
			function Equals($x, $y)
			{
				return ($x == $y);
			}
			
			function GetHashCode($x)
			{
				return $x->GetHashCode();
			}
		}
	
		class Interop
		{
			// The following method is "exported" which means that overloads callable from other .NET languages
			// will be generated. The signatures are also determined by PHP type hints if they are present.
			// 
			// Two overloads will be generated in this case (C# syntax):
			// object f(object a, ref object b, int c, ref string d, out double e);
			// object f(object a, ref object b, int c, ref string d, out double e, ICollection f);
			
			[\Export]
			function f($a, &$b, int $c, string &$d, [\Out]double &$e, \System\Collections\ICollection $f = NULL)
			{
				echo "f invoked with arguments:";
				echo "$a, $b, $c, $d, $e, $f";
				
				// change the arguments passed by ref
				$b = 123;
				$d = "abc";
				$e = 3.14;
				
				return "OK";
			}
		
			// The following field is also exported. It will be exposed as a property of the respective name.
			// The generated accessors will do all the necessary conversions from CLR to PHP and vice versa.
		
			[\Export]
			public $x;
		
			static function Run()
			{
				echo "Calling a BCL method with out parameter:\n";
				
				$x = 0;

				echo "before: x = $x\n";
				S\Int32::TryParse("1", $x);
				echo "after: x = $x\n";
				
				echo "\n";
				echo "Calling a BCL method with 'params' variable number of arguments:\n";
				
				echo i'S\String'::Format("{0} {1} {2} {3} {4} {5} {6}\n\n",
					1,
					1.1,
					false,
					"test",
					S\DateTime::$Now,
					S\Environment::$TickCount,
					S\AppDomain::$CurrentDomain->FriendlyName);
			
				echo "Calling an exported field accessor:\n";
			
				// let's find and invoke the exported field accessor via Reflection
				$property = S\Reflection\Assembly::GetEntryAssembly()->GetType("Phalanger.Interop")->GetProperty("x");

				$a = new Interop();
				$a->x = 123;
				
				echo "before: a.x = $a->x\n";
				$property->SetValue($a, 987, array());
				echo "after: a.x = $a->x\n";
				
				echo "\n\n";
			}
		}
	}
?>
