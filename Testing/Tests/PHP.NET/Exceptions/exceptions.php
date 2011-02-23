<?
import namespace System;
import namespace System:::Windows:::Forms;

if (1==1)
{
	class MyException extends Exception
	{
		 // Redefine the exception so message isn't optional
		 public function __construct($message, $code = 0) {
				 // some code
	   
				 // make sure everything is assigned properly
				 parent::__construct($message, $code);
		 }

		 // custom string representation of object
		 public function __toString() {
				 return __CLASS__ . ": [{$this->code}]: {$this->message}\n";
		 }

		 public function customFunction() {
				 echo "A Custom function for this type of exception\n";
		 }
	}
}

try
{
	throw new MyException("My message.");
}
catch(Exception $e)
{
	echo "PHP Exception: ".($e->getMessage());
}

try
{
	Int32::Parse("aa");
}
catch(System:::Exception $e)
{
	echo "\nCLR Exception: ".($e->Message);
}

?>
