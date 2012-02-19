<?php

    // timing functions
	require_once '..\timing.php';

    // tests:

	class X
	{
	}

	class Y
	{
		function __construct()
		{ }
	}

	class Z
	{
		static $a;
		var $b;
		
		static function m()
		{ }
		
		function n()
		{ }
	}
	
	class A1
	{
		var $v1;
		
		function f1()
		{ }
	}
	
	class A2 extends A1
	{
		var $v2;
		
		function f2()
		{ }
	}
	
	class A3 extends A2
	{
		var $v3;
		
		function f3()
		{ }
	}
	
	eval('class DynamicClass{}');
	
	class Start
	{
		const LOOP_COUNT = 10000000;
	
		static function Main()
		{
			for ($k = 1; $k <= 3; $k++)
			{
				echo "Benchmark #$k\n";
				echo "============\n";
			
				self::EmptyLoop();
				
				self::UnoptimalizedLoop();
				self::StaticFields();
				self::InstanceFields();
				
				self::StaticMethods();
				self::InstanceMethods1();
				self::InstanceMethods2();
				
				self::FieldInheritance();
				self::MethodInheritance();
				
				self::Operators();
				self::Arrays();
				
				self::Instantiation();
			}
            
            Timing::OutputResults();
		}

        static function EmptyLoop()
        {
            for ($j = 0; $j < 3; $j++)
			{
				Timing::Start("Empty loop");
				for ($i = 0; $i < self::LOOP_COUNT; $i++) { }
				Timing::Stop();
			}
        }

		static function UnoptimalizedLoop()
		{
			Timing::Start("Empty unoptimalized loop");
			$i = 'i2';
			for(${$i} = 0; ${$i} < self::LOOP_COUNT; ${$i} ++){}
			Timing::Stop();
		}

		static function StaticFields()
		{
			Timing::Start("Static field direct read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = Z::$a;
			Timing::Stop();

			Timing::Start("Static field direct write");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) Z::$a = $_x;
			Timing::Stop();

			$_y = "a";
			Timing::Start("Static field indirect read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = Z::$$_y;
			Timing::Stop();

			$_y = "a";
			Timing::Start("Static field indirect write");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) Z::$$_y = $_x;
			Timing::Stop();
		}
		
		static function InstanceFields()
		{
			$_y = new Z;
			Timing::Start("Instance field direct read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->b;
			Timing::Stop();

			$_y = new Z;
			Timing::Start("Instance field direct write");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y->b = $_x;
			Timing::Stop();

			$_y = new Z;
			$_z = "b";
			Timing::Start("Instance field indirect read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->$_z;
			Timing::Stop();

			$_y = new Z;
			$_z = "b";
			Timing::Start("Instance field indirect write");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y->$_z = $_x;
			Timing::Stop();

			$_y = new Z;
			$_y->c = NULL;
			Timing::Start("Undeclared instance field direct read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->c;
			Timing::Stop();

			$_y = new Z;
			$_y->c = NULL;
			Timing::Start("Undeclared instance field direct write");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y->c = $_x;
			Timing::Stop();

			$_y = new Z;
			$_z = "c";
			$_y->c = NULL;
			Timing::Start("Undeclared instance field indirect read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->$_z;
			Timing::Stop();

			$_y = new Z;
			$_z = "c";
			$_y->c = NULL;
			Timing::Start("Undeclared instance field indirect write");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y->$_z = $_x;
			Timing::Stop();
		}
		
		static function StaticMethods()
		{
			Timing::Start("Static method direct invocation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) Z::m();
			Timing::Stop();

			$_y = "m";
			Timing::Start("Static method indirect invocation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) Z::$_y();
			Timing::Stop();
		}

		static function InstanceMethods1()
		{
			$_y = new Z;
			Timing::Start("Instance method direct invocation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y->n();
			Timing::Stop();
		}

		static function InstanceMethods2()
		{
			$_y = new Z;
			$_z = "n";
			Timing::Start("Instance method indirect invocation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y->$_z();
			Timing::Stop();
		}

		static function FieldInheritance()
		{
			$_y = new A3;
			Timing::Start("Declared instance field direct read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->v3;
			Timing::Stop();

			$_y = new A3;
			Timing::Start("Inherited instance field direct read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->v2;
			Timing::Stop();

			$_y = new A3;
			Timing::Start("Inherited (two levels) instance field direct read");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->v1;
			Timing::Stop();
		}

		static function MethodInheritance()
		{
			$_y = new A3;
			Timing::Start("Declared instance method direct invocation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->f3();
			Timing::Stop();

			$_y = new A3;
			Timing::Start("Inherited instance method direct invocation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->f2();
			Timing::Stop();

			$_y = new A3;
			Timing::Start("Inherited (two levels) instance method direct invocation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y->f1();
			Timing::Stop();
		}

		static function Operators()
		{
			$_x = 0;
			Timing::Start("Variable increment");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x++;
			Timing::Stop();

			$_y = "ahoj";
			$_z = "babi";
			Timing::Start("String concatenation");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y . $_z;
			Timing::Stop();
		}

		static function Arrays()
		{
			$_y = array(123 => "hujer", "abcd" => "hujer");
			Timing::Start("Array item direct read (int key)");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y[123];
			Timing::Stop();

			$_y = array(123 => "hujer", "abcd" => "hujer");
			Timing::Start("Array item direct write (int key)");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y[123] = $_x;
			Timing::Stop();

			$_y = array(123 => "hujer", "abcd" => "hujer");
			Timing::Start("Array item direct read (string key)");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = $_y["abcd"];
			Timing::Stop();

			$_y = array(123 => "hujer", "abcd" => "hujer");
			Timing::Start("Array item direct write (string key)");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_y["abcd"] = $_x;
			Timing::Stop();
		}

		static function Instantiation()
		{
			Timing::Start("Instantiate - no ctor");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = new X;
			Timing::Stop();
			
			Timing::Start("Instantiate - empty ctor");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = new Y;
			Timing::Stop();
			
			Timing::Start("Instantiate - dynamic new");
			for ($i = 0; $i < self::LOOP_COUNT; $i++) $_x = new DynamicClass;
			Timing::Stop();
		}
		
	}
?>
