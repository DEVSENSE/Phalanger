<?
	import namespace System:::Collections:::Generic;
  import namespace System;
  
  [My("class")]
	class A<:[My("typeargX")]X,Y:>
	{
		const Hello = "Hello";
		
		function __construct()
		{
			echo "hello";	
		}
		
		[My("ftion")]
		function [My("retval")] &g<:U,[My("typeargV")]V,W:>([Out,My("parameter")] U &$x)
		{
		}
		
		function f<:A = System:::String,B = Y,C = int:>()
		{
		}
	}
	
	[AttributeUsage(AttributeTargets::All)]
	class MyAttribute extends Attribute
	{
		function __construct($x)
		{
		} 
	}
	
	class Program
	{
		function Main()
		{
		}
	}
?>