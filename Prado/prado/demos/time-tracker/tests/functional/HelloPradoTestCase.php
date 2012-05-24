<?php

//web testing
class HelloPradoTestCase extends SeleniumTestCase
{
	function testIndexPage()
	{
		$this->open('../index.php');
		$this->assertTextPresent('Welcome to Prado!');
		//add more test assertions...
	}
}

?>