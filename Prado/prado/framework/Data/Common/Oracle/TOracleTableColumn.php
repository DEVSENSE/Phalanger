<?php
/**
 * TOracleTableColumn class file.
 *
 * @author Marcos Nobre <marconobre[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TOracleTableColumn.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Data.Common.Oracle
 */

/**
 * Load common TDbTableCommon class.
 */
Prado::using('System.Data.Common.TDbTableColumn');

/**
 * Describes the column metadata of the schema for a PostgreSQL database table.
 *
 * @author Marcos Nobre <marconobre[at]gmail[dot]com>
 * @version $Id: TOracleTableColumn.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Data.Common.Oracle
 * @since 3.1
 */
class TOracleTableColumn extends TDbTableColumn
{
	private static $types=array(
		'numeric' => array( 'numeric' )		
//		'integer' => array('bit', 'bit varying', 'real', 'serial', 'int', 'integer'),
//		'boolean' => array('boolean'),
//		'float' => array('bigint', 'bigserial', 'double precision', 'money', 'numeric')
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

?>
