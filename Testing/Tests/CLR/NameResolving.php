[pure]
[expect exact]
bool(true)
bool(true)
object(Directory)(2) {
  ["handle"]=>
  NULL
  ["path"]=>
  NULL
}
[file]
<?
	import namespace System;
	
	namespace N
	{
		class Beth { } 
	}
	
	namespace
	{
		use N\Beth;
		use System\Collections\Generic\Dictionary;
		
		class Program
		{
			public static function Main()
			{
				var_dump(new \N\Beth instanceof Beth);
				var_dump(new Dictionary() instanceof Dictionary);
				var_dump(new Directory);
			}
		}
	}	
?>