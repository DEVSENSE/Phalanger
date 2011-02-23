[expect php]
[file]
<?php
include('Phalanger.inc');
include('Autoload.inc');


function unserializer($class_name)
{
	echo __METHOD__ . "($class_name)\n";
	switch($class_name)
	{
	case 'TestNAOld':
		eval("class TestNAOld extends TestOld {}");
		break;
	case 'TestNANew':
		eval("class TestNANew extends TestNew {}");
		break;
	case 'TestNANew2':
		eval("class TestNANew2 extends TestNew {}");
		break;
	default:
		echo "Try __autoload()\n";
		__autoload($class_name);
		break;
	}
}

ini_set('unserialize_callback_func', 'unserializer');

class TestOld
{
	function serialize()
	{
		echo __METHOD__ . "()\n";
	}
	
	function unserialize($serialized)
	{
		echo __METHOD__ . "()\n";
	}
	
	function __wakeup()
	{
		echo __METHOD__ . "()\n";
	}
	
	function __sleep()
	{
		echo __METHOD__ . "()\n";
		return array();
	}
}

class TestNew implements Serializable
{
	protected static $check = 0;

	function serialize()
	{
		echo __METHOD__ . "()\n";
		switch(++self::$check)
		{
		case 1:
			return NULL;
		case 2:
			return "2";
		}
	}
	
	function unserialize($serialized)
	{
		echo __METHOD__ . "()\n";
	}
	
	function __wakeup()
	{
		echo __METHOD__ . "()\n";
	}
	
	function __sleep()
	{
		echo __METHOD__ . "()\n";
	}
}

echo "===O1===\n";
__var_dump($ser = serialize(new TestOld));
__var_dump(unserialize($ser));

echo "===N1===\n";
__var_dump($ser = serialize(new TestNew));
__var_dump(unserialize($ser));

echo "===N2===\n";
__var_dump($ser = serialize(new TestNew));
__var_dump(unserialize($ser));

echo "===NAOld===\n";
__var_dump(unserialize('O:9:"TestNAOld":0:{}'));

echo "===NANew===\n";
__var_dump(unserialize('O:9:"TestNANew":0:{}'));

echo "===NANew2===\n";
__var_dump(unserialize('C:10:"TestNANew2":0:{}'));

echo "===AutoOld===\n";
__var_dump(unserialize('O:19:"autoload_implements":0:{}'));

// Now we have __autoload(), that will be called before the old style header.
// If the old style handler also fails to register the class then the object
// becomes an incomplete class instance.

echo "===AutoNA===\n";
__var_dump(@unserialize('O:22:"autoload_not_available":0:{}'));
?>