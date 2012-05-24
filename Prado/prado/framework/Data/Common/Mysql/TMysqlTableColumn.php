<?php
/**
 * TMysqlTableColumn class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TMysqlTableColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.Common.Mysql
 */

/**
 * Load common TDbTableCommon class.
 */
Prado::using('System.Data.Common.TDbTableColumn');

/**
 * Describes the column metadata of the schema for a Mysql database table.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TMysqlTableColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.Common.Mysql
 * @since 3.1
 */
class TMysqlTableColumn extends TDbTableColumn
{
	private static $types = array(
		'integer' => array('bit', 'tinyint', 'smallint', 'mediumint', 'int', 'integer', 'bigint'),
		'boolean' => array('boolean', 'bool'),
		'float' => array('float', 'double', 'double precision', 'decimal', 'dec', 'numeric', 'fixed')
		);

	/**
	 * Overrides parent implementation, returns PHP type from the db type.
	 * @return boolean derived PHP primitive type from the column db type.
	 */
	public function getPHPType()
	{
		$dbtype = trim(str_replace(array('unsigned', 'zerofill'),array('','',),strtolower($this->getDbType())));
		if($dbtype==='tinyint' && $this->getColumnSize()===1)
			return 'boolean';
		foreach(self::$types as $type => $dbtypes)
		{
			if(in_array($dbtype, $dbtypes))
				return $type;
		}
		return 'string';
	}

	/**
	 * @return boolean true if column will auto-increment when the column value is inserted as null.
	 */
	public function getAutoIncrement()
	{
		return $this->getInfo('AutoIncrement', false);
	}

	/**
	 * @return boolean true if auto increment is true.
	 */
	public function hasSequence()
	{
		return $this->getAutoIncrement();
	}

	public function getDbTypeValues()
	{
		return $this->getInfo('DbTypeValues');
	}
}

