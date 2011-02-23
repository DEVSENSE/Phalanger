class Program
{
	public static function Main()
	{
		$x = 0;
		
		there:
				
		goto here;
	
		echo "blah\n";
		
		here:
		
		echo "hujer\n";
		
		if ($x++ < 10) 
			goto there;
	}
}	