<?
	// This script demonstrates the generics support in Phalanger.

	namespace Phalanger
	{
        use System\Collections\Generic\IComparer;

        use System as S;
        use System\Collections\Generic as G;

        class Displayer<:T:>
		{
			private static function Action($element)
			{
				echo "element: $element\n";
			}
		
			static function Display(i'G\List'<:T:> $list)
			{
				// creating an instance of a generic delegate
				$action = new S\Action<:T:>(array("self", "Action"));
				$list->ForEach($action);
			}
		}
		
		// Generic class with two generic parameters:
		class C1<:T, U:>
		{ }
		
		// Generic class with one generic parameter:
		class C2<:T:> extends C1<:T, bool:>
		{ }
		
		// Non-generic class:
		class C3 extends C1<:string, double:>
		{ }

		// Generic class implementing BCL generic interface. The generic parameter has a default
		// value, which means that in PHP the generic parameter can be ommitted.
		class MyGenericComparer<:T = array:> implements IComparer<:T:>
		{
			function Compare($x, $y)
			{
				if ($x < $y) return -1;
				if ($x > $y) return 1;
				return 0;
			}
		}
	
		class Generics
		{
			// The following generic method has a default generic parameter. If it is called without
			// generic argument, string will be used.
			// 
			// Two export overloads are generated (C# syntax):
			// object GenericMethod(string arg1);
			// object GenericMethod<T>(T arg1);
		
			[\Export]
			static function GenericMethod<:T = string:>(T $arg1)
			{
				$dict = new G\Dictionary<:string, T:>;
				$dict->Add("test", $arg1);
				
				return $dict;
			}
		
			static function Run()
			{
				echo "Creating an instance of a generic type instantiation:\n";
				
				// list is a keyword so we need to escape it using i''
				$x = new i'G\List'<:int:>;
				echo $x->GetType()->FullName . "\n";
				
				$x->Add(2);
				$x->Add(3);
				$x->Add(5);
				$x->Add(7);
				$x->Add(11);
				
				echo "\nEnumerating via generic delegate:\n";			
				Displayer<:int:>::Display($x);
				
				echo "\nEnumerating via foreach:\n";
				foreach ($x as $element)
				{
					echo "element: $element\n";
				}

				echo "\nCalling BCL generic method:\n";
				
				// no need to explicitly create the delegate, just pass the target designation
				$all_primes = i'S\Array'::TrueForAll<:int:>($x->ToArray(), array("self", "IsPrime"));
				echo ($all_primes ? "All elements are primes" : "Nope");
				echo "\n";
				
				echo "\nCalling generic method with default generic argument:\n";
				
				$y = self::GenericMethod("hello");
				echo $y->GetType()->FullName . "\n";
				
				echo "\nCalling generic method with generic argument supplied at run-time:\n";
				
				$args = array(
					array("@int", -10),
					array("@bool", true),
					array("@string", "xyz"),
					array("System\EventArgs", S\EventArgs::$Empty)
				);
				
				foreach ($args as $arg)
				{
					// we are dynamic!
					$dict = self::GenericMethod<:$arg[0]:>($arg[1]);
					
					foreach ($dict as $element)
					{
						echo S\Convert::ToString($element) . "\n";
					}
				}
				
				echo "\n\n";
			}
			
			// Called by Array::TrueForAll
			private static function IsPrime($n)
			{
				echo "checking element: $n\n";
			
				if ($n < 2) return false;
				for ($i = 2; ($i * $i) < $n; $i++)
				{
					if (($n % $i) == 0) return false;
				}
				
				return true;
			}
		}
	}
?>
