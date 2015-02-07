[pure]
[expect exact]
bool(false)
bool(true)
object(Directory)(2)
{
  ["handle"] => NULL
  ["path"] => NULL
}
[file]
<?
	namespace N
	{
		class Beth { } 
	}
	
	namespace
	{
		use System\Collections\Dictionary as Dictionary;
		
		class Program
		{
			public static function Main()
			{
				var_dump(new N\Beth instanceof Beth);
				var_dump(new Dictionary\KeyCollection(new Dictionary) instanceof Dictionary\KeyCollection);
				var_dump(new Directory);
			}
		}
	}	
?>