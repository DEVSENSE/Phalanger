<?php


Prado::using('Application.App_Code.Dao.*');

class BaseTestCase extends UnitTestCase
{
	protected $sqlmap;

	function setup()
	{
		$app = Prado::getApplication();
		$this->sqlmap = $app->getModule('daos')->getClient();
	}


	function flushDatabase()
	{
		$conn = $this->sqlmap->getDbConnection();
		$find = 'sqlite:protected';
		if(is_int(strpos($conn->getConnectionString(),$find)))
			$conn->ConnectionString = str_replace($find, 'sqlite:../protected', $conn->ConnectionString);
		$conn->setActive(false);
		$conn->setActive(true);
		switch(strtolower($conn->getDriverName()))
		{
			case 'mysql':
			return $this->flushMySQLDatabase();
			case 'sqlite':
			return $this->flushSQLiteDatabase();
		}
	}

	function flushSQLiteDatabase()
	{
		$conn = $this->sqlmap->getDbConnection();
		$file = str_replace('sqlite:','',$conn->getConnectionString());
		$backup = $file.'.bak';
		copy($backup, $file);
	}

	function flushMySQLDatabase()
	{
		$conn = $this->sqlmap->getDbConnection();
		$file = Prado::getPathOfNamespace('Application.App_Data.MySQL4.mysql-reset','.sql');
		if(is_file($file))
			$this->runScript($conn, $file);
		else
			throw new Exception('unable to find script file '.$file);
	}

	protected function runScript($connection, $script)
	{
		$sql = file_get_contents($script);
		$lines = explode(';', $sql);
		foreach($lines as $line)
		{
			$line = trim($line);
			if(strlen($line) > 0)
				$connection->createCommand($line)->execute();
		}
	}
}
?>