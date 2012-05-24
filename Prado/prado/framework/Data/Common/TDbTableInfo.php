<?php
/**
 * TDbTableInfo class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbTableInfo.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 */

/**
 * TDbTableInfo class describes the meta data of a database table.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDbTableInfo.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 * @since 3.1
 */
class TDbTableInfo extends TComponent
{
	private $_info=array();

	private $_primaryKeys;
	private $_foreignKeys;

	private $_columns;

	private $_lowercase;

	/**
	 * @var null|array
	 * @since 3.1.7
	 */
	private $_names = null;

	/**
	 * Sets the database table meta data information.
	 * @param array table column information.
	 */
	public function __construct($tableInfo=array(),$primary=array(),$foreign=array())
	{
		$this->_info=$tableInfo;
		$this->_primaryKeys=$primary;
		$this->_foreignKeys=$foreign;
		$this->_columns=new TMap;
	}

	/**
	 * @param TDbConnection database connection.
	 * @return TDbCommandBuilder new command builder
	 */
	public function createCommandBuilder($connection)
	{
		Prado::using('System.Data.Common.TDbCommandBuilder');
		return new TDbCommandBuilder($connection,$this);
	}

	/**
	 * @param string information array key name
	 * @param mixed default value if information array value is null
	 * @return mixed information array value.
	 */
	protected function getInfo($name,$default=null)
	{
		return isset($this->_info[$name]) ? $this->_info[$name] : $default;
	}

	/**
	 * @param string information array key name
	 * @param mixed new information array value.
	 */
	protected function setInfo($name,$value)
	{
		$this->_info[$name]=$value;
	}

	/**
	 * @return string name of the table this column belongs to.
	 */
	public function getTableName()
	{
		return $this->getInfo('TableName');
	}

	/**
	 * @return string full name of the table, database dependent.
	 */
	public function getTableFullName()
	{
		return $this->getTableName();
	}

	/**
	 * @return boolean whether the table is a view, default is false.
	 */
	public function getIsView()
	{
		return $this->getInfo('IsView',false);
	}

	/**
	 * @return TMap TDbTableColumn column meta data.
	 */
	public function getColumns()
	{
		return $this->_columns;
	}

	/**
	 * @param string column id
	 * @return TDbTableColumn column information.
	 */
	public function getColumn($name)
	{
		if(($column = $this->_columns->itemAt($name))!==null)
			return $column;
		throw new TDbException('dbtableinfo_invalid_column_name', $name, $this->getTableFullName());
	}

	/**
	 * @param array list of column Id, empty to get all columns.
	 * @return array table column names (identifier quoted)
	 */
	public function getColumnNames()
	{
		if($this->_names===null)
		{
			$this->_names=array();
			foreach($this->getColumns() as $column)
				$this->_names[] = $column->getColumnName();
		}
		return $this->_names;
	}

	/**
	 * @return string[] names of primary key columns.
	 */
	public function getPrimaryKeys()
	{
		return $this->_primaryKeys;
	}

	/**
	 * @return array tuples of foreign table and column name.
	 */
	public function getForeignKeys()
	{
		return $this->_foreignKeys;
	}

	/**
	 * @return array lowercased column key names mapped to normal column ids.
	 */
	public function getLowerCaseColumnNames()
	{
		if($this->_lowercase===null)
		{
			$this->_lowercase=array();
			foreach($this->getColumns()->getKeys() as $key)
				$this->_lowercase[strtolower($key)] = $key;
		}
		return $this->_lowercase;
	}
}
