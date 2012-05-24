<?php
/**
 * TPgsqlTableColumn class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPgsqlTableColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.Common.Pgsql
 */

/**
 * Load common TDbTableCommon class.
 */
Prado::using('System.Data.Common.TDbTableColumn');

/**
 * Describes the column metadata of the schema for a PostgreSQL database table.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TPgsqlTableColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.Common.Pgsql
 * @since 3.1
 */
class TPgsqlTableColumn extends TDbTableColumn
{
	private static $types=array(
		'integer' => array('bit', 'bit varying', 'real', 'serial', 'int', 'integer'),
		'boolean' => array('boolean'),
		'float' => array('bigint', 'bigserial', 'double precision', 'money', 'numeric')
	);

	/**
	 * Overrides parent implementation, returns PHP type from the db type.
	 * @return boolean derived PHP primitive type from the column db type.
	 */
	public function getPHPType()
	{
		$dbtype = strtolower($this->getDbType());
		foreach(self::$types as $type => $dbtypes)
		{
			if(in_array($dbtype, $dbtypes))
				return $type;
		}
		return 'string';
	}
}

