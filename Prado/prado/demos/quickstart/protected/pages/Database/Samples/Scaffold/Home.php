<?php

Prado::using('System.Data.ActiveRecord.TActiveRecord');
Prado::using('System.Data.ActiveRecord.Scaffold.TScaffoldView');

class AddressRecord extends TActiveRecord
{
	const TABLE='addresses';

	public $id;
	public $username;
	public $phone;

	//for demo, we use static db here
	//otherwise we should use TActiveRecordConfig in application.xml
	private static $_db;
	public function getDbConnection()
	{
		if(self::$_db===null)
		{
			$file = dirname(__FILE__).'/sqlite.db';
			self::$_db = new TDbConnection("sqlite:{$file}");
		}
		return self::$_db;
	}
}

class Home extends TPage
{

}

?>