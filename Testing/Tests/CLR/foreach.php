[pure]
[expect exact]
1, 2, 3, 4, 5, OK
[file]
<?php

use System\Collections\Generic as G;

class P
{
	static function CreateList<:T:>($vals)
	{
		$ret = new i'System\Collections\Generic\List'<:T:>;
		foreach($vals as $v)
			$ret->Add($v);
		return $ret;		
	}

	public static function Main()
	{
		$l = self::CreateList<:\System\Int32:>(array(1,2,3,4,5));

		// This causes Common Langauge Runtime 
		// TODO: (J) keyed enumeration of non-keyed collection // foreach($l as $key => $value) echo "$value, ";
		foreach($l as $value) echo "$value, ";

		echo "OK";
	}
}

?>