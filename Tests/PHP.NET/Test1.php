<?
/*	import namespace System;
	import namespace System:::Reflection;
	import namespace System:::Collections:::Generic;
	import namespace System:::Web;

/*

#pragma file C:\A\B\x.php
#pragma line 500

class C extends C
{
}

#pragma file Q:\sadasd


class E extends U #pragma line 1000
{
}

#pragma default line
#pragma default file

class D extends U
{
}
*/


// TODO: error
//interface I1 { function fF(); }
//interface I2 extends I1 { function Ff(); }
//class X implements I2 { function ff()  {} }

// TODO: error
	//interface test 
	//{
		//public function __construct($foo);
	//}
//
	//class foo implements test 
	//{
		//public function __construct() 
		//{
		//}
	//}

// TODO: error
//class C
//{
	//private function f() { }
//}
//
//class D extends C
//{
	//function f() { }
//}

/*
			
/*	class MyPage extends System:::Web:::UI:::Page
	{
		protected function FrameworkInitialize()
		{
			parent::FrameworkInitialize();
		}
	}
*/
	/*class C<:X:>
	{
		const X = Z;
		
		static function Q()
		{
			$x = "Dictionary<:!X,List<:string:>:>";
			$y = new $x;
			$y->Add(1, new i'List'<:string:>());
			
		}
	}*/

	class Start
	{
		static function f<:T:>()
		{
			var_dump(new System:::Collections:::Generic:::Dictionary<:string, T:>);
		}
	}

	
	class Program
	{
		public static function Main()
		{
			fgets(STDIN);
		}
	}
	
	echo "Hello";
	Start::f<:Program:>();
	
	$a="a";
	extract($GLOBALS, EXTR_REFS);
	echo "ok\n";
	
?>