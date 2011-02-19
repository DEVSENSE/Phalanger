<?
  import namespace System:::Collections:::Generic;
  import namespace System;
  
  class B<:T = int:>
  {
  }
  
  class C<:A:>
  {
  }
  
  class Program
	{
		static function Main()
		{
			self::InstaceOfs<:string:>();
		}
		
		static function InstanceOfs<:T:>()
		{
			echo "---------";
			
			echo new int instanceof int;

			echo "\n---------\n";

			echo new T instanceof T;
			
			echo "\n---------\n";

			echo new T<:int:> instanceof T<:int:>;
			
			echo "\n---------\n";

			echo new B<:T:> instanceof B<:T:>;

			echo "\n---------\n";

			echo new T<:T:> instanceof T<:T:>;

			echo "\n---------\n";

			echo new C<:int:> instanceof C<:int:>;

			echo "\n---------\n";

			echo new Unk instanceof Unk;

			echo "\n---------\n";

			echo new Unk<:int, string, bool, double:> instanceof Unk<:int, string, bool, double:>;

			echo "\n---------\n";

			echo new Unk1<:int, Unk2<:string:>:> instanceof Unk1<:int, Unk2<:string:>:>;

			echo "\n---------\n";

			echo new T<:Unk1:> instanceof T<:Unk1:>;

			echo "\n---------\n";

			echo new T<:int, T<:Unk:>:> instanceof T<:int, T<:Unk:>:>;

			echo "\n---------\n";

			echo new T<:int, C<:Unk:>:> instanceof T<:int, C<:Unk:>:>;

			echo "\n---------\n";

			if ($x)
			{
				class D<:W:> {}
			}
			else if ($y)
			{
				class D<:W1,W2:> { }
			}	
			else
			{
				class D { }
			}
			
			echo new D instanceof D;
			echo "\n---------\n";

			echo new D<:int:> instanceof D<:int:>;

			echo "\n---------\n";

			echo new D<:int, string:> instanceof D<:int, string:>;

			echo "\n---------\n";

			echo new C<:C<:D:>:> instanceof C<:C<:D:>:>;

			echo "\n---------\n";

			$t = "int";

			echo new C<:$t:> instanceof C<:$t:>;
			
			echo "\n---------\n";

			$t = "C";
			
			echo new $t<:$t:> instanceof $t<:$t:>;

			echo "\n---------\n";

			$t = "C<:int, C<:int, string:>, D<:D:> :>";
			
			echo new $t instanceof $t;
			
			echo "\n---------\n";

			$t = "C<:int, C<:int, string:, D<:D:> :>";
			
			echo new $t instanceof $t;
			
			echo "\n---------\n";

			$t = "C<:int, C<::> :>";
			
			echo new $t instanceof $t;
			
			echo "\n---------\n";

			echo new B<:int:> instanceof B<:int:>;

			echo "\n---------\n";

			echo new B<:B:> instanceof B<:B:>;

			echo "\n---------\n";
			
			echo new B<:B<:string:>:> instanceof B<:B<:string:>:>;

			echo "\n---------\n";
			echo new Dictionary<:int, string:> instanceof Dictionary<:int, string:>;

			echo "\n---------\n";
			
			echo new Dictionary<:int:> instanceof Dictionary<:int:>;

			echo "\n---------\n";
			
			echo new Dictionary instanceof Dictionary;	

			echo "\n---------\n";
			
			// Error: new IComparable; 
			echo $x instanceof IComparable;

			echo "\n---------\n";
			
			// Error: new IComparable<:Dictionary:>;
			echo $x instanceof IComparable<:Dictionary:>;

			echo "\n---------\n";
			
			// Error: echo  $x instanceof Dictionary<:int, int, int:>;
			// Syntax Error: $x = int<:int:>;

			echo "\n---------\n";
		}
	}	
?>
