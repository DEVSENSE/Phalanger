<?php
/**
 * TDbTableColumn class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbTableColumn.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 */

/**
 * TDbTableColumn class describes the column meta data of the schema for a database table.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDbTableColumn.php 2920 2011-05-21 19:29:39Z ctrlaltca@gmail.com $
 * @package System.Data.Common
 * @since 3.1
 */
class TDbTableColumn extends TComponent
{
	const UNDEFINED_VALUE= INF; //use infinity for undefined value

	private $_info=array();

	/**
	 * Sets the table column meta data.
	 * @param array table column information.
	 */
	public function __construct($columnInfo)
	{
		$this->_info=$columnInfo;
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
	 * Returns the derived PHP primitive type from the db type. Default returns 'string'.
	 * @return string derived PHP primitive type from the column db type.
	 */
	public function getPHPType()
	{
		return 'string';
	}

	/**
	 * @param integer PDO bind param/value types, default returns string.
	 */
	public function getPdoType()
	{
		switch($this->getPHPType())
		{
			case 'boolean': return PDO::PARAM_BOOL;
			case 'integer': return PDO::PARAM_INT;
			case 'string' : return PDO::PARAM_STR;
		}
		return PDO::PARAM_STR;
	}

	/**
	 * @return string name of the column in the table (identifier quoted).
	 */
	public function getColumnName()
	{
		return $this->getInfo('ColumnName');
	}

	/**
	 * @return string name of the column with quoted identifier.
	 */
	public function getColumnId()
	{
		return $this->getInfo('ColumnId');
	}

	/**
	 * @return string size of the column.
	 */
	public function getColumnSize()
	{
		return $this->getInfo('ColumnSize');
	}

	/**
	 * @return integer zero-based ordinal position of the column in the table.
	 */
	public function getColumnIndex()
	{
		return $this->getInfo('ColumnIndex');
	}

	/**
	 * @return string column type.
	 */
	public function getDbType()
	{
		return $this->getInfo('DbType');
	}

	/**
	 * @return boolean specifies whether value Null is allowed, default is false.
	 */
	public function getAllowNull()
	{
		return $this->getInfo('AllowNull',false);
	}

	/**
	 * @return mixed default column value if column value was null.
	 */
	public function getDefaultValue()
	{
		return $this->getInfo('DefaultValue', self::UNDEFINED_VALUE);
	}

	/**
	 * @return string precision of the column data, if the data is numeric.
	 */
	public function getNumericPrecision()
	{
		return $this->getInfo('NumericPrecision');
	}

	/**
	 * @return string scale of the column data, if the data is numeric.
	 */
	public function getNumericScale()
	{
		return $this->getInfo('NumericScale');
	}

	public function getMaxiumNumericConstraint()
	{
		if(($precision=$this->getNumericPrecision())!==null)
		{
			$scale=$this->getNumericScale();
			return $scale===null ? pow(10,$precision) : pow(10,$precision-$scale);
		}
	}

	/**
	 * @return boolean whether this column is a primary key for the table, default is false.
	 */
	public function getIsPrimaryKey()
	{
		return $this->getInfo('IsPrimaryKey',false);
	}

	/**
	 * @return boolean whether this column is a foreign key, default is false.
	 */
	public function getIsForeignKey()
	{
		return $this->getInfo('IsForeignKey',false);
	}

	/**
	 * @param string sequence name, only applicable if column is a sequence
	 */
	public function getSequenceName()
	{
		return $this->getInfo('SequenceName');
	}

	/**
	 * @return boolean whether the column is a sequence.
	 */
	public function hasSequence()
	{
		return $this->getSequenceName()!==null;
	}

	/**
	 * @return boolean whether this column is excluded from insert and update.
	 */
	public function getIsExcluded()
	{
		return false;
	}
}

