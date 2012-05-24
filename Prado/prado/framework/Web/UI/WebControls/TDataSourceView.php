<?php
/**
 * TDataSourceSelectParameters, TDataSourceView, TReadOnlyDataSourceView class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDataSourceView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TDataSourceSelectParameters class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataSourceView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TDataSourceSelectParameters extends TComponent
{
	private $_retrieveTotalRowCount=false;
	private $_startRowIndex=0;
	private $_totalRowCount=0;
	private $_maximumRows=0;

	public function getStartRowIndex()
	{
		return $this->_startRowIndex;
	}

	public function setStartRowIndex($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			$value=0;
		$this->_startRowIndex=$value;
	}

	public function getMaximumRows()
	{
		return $this->_maximumRows;
	}

	public function setMaximumRows($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			$value=0;
		$this->_maximumRows=$value;
	}

	public function getRetrieveTotalRowCount()
	{
		return $this->_retrieveTotalRowCount;
	}

	public function setRetrieveTotalRowCount($value)
	{
		$this->_retrieveTotalRowCount=TPropertyValue::ensureBoolean($value);
	}

	public function getTotalRowCount()
	{
		return $this->_totalRowCount;
	}

	public function setTotalRowCount($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			$value=0;
		$this->_totalRowCount=$value;
	}
}

/**
 * TDataSourceView class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataSourceView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
abstract class TDataSourceView extends TComponent
{
	private $_owner;
	private $_name;

	public function __construct(IDataSource $owner,$viewName)
	{
		$this->_owner=$owner;
		$this->_name=$viewName;
	}

	/**
	 * Performs DB selection based on specified parameters.
	 * @param ???
	 * @return Traversable
	 */
	abstract public function select($parameters);

	/**
	 * Inserts a DB record.
	 * @param array|TMap
	 * @return integer affected rows
	 */
	public function insertAt($values)
	{
		throw new TNotSupportedException('datasourceview_insert_unsupported');
	}

	/**
	 * Updates DB record(s) with the specified keys and new values
	 * @param array|TMap keys for specifying the records to be updated
	 * @param array|TMap new values
	 * @return integer affected rows
	 */
	public function update($keys,$values)
	{
		throw new TNotSupportedException('datasourceview_update_unsupported');
	}

	/**
	 * Deletes DB row(s) with the specified keys.
	 * @param array|TMap keys for specifying the rows to be deleted
	 * @return integer affected rows
	 */
	public function delete($keys)
	{
		throw new TNotSupportedException('datasourceview_delete_unsupported');
	}

	public function getCanDelete()
	{
		return false;
	}

	public function getCanInsert()
	{
		return false;
	}

	public function getCanPage()
	{
		return false;
	}

	public function getCanGetRowCount()
	{
		return false;
	}

	public function getCanSort()
	{
		return false;
	}

	public function getCanUpdate()
	{
		return false;
	}

	public function getName()
	{
		return $this->_name;
	}

	public function getDataSource()
	{
		return $this->_owner;
	}

	public function onDataSourceViewChanged($param)
	{
		$this->raiseEvent('OnDataSourceViewChanged',$this,$param);
	}
}

/**
 * TReadOnlyDataSourceView class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataSourceView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TReadOnlyDataSourceView extends TDataSourceView
{
	private $_dataSource=null;

	public function __construct(IDataSource $owner,$viewName,$dataSource)
	{
		parent::__construct($owner,$viewName);
		if($dataSource===null || is_array($dataSource))
			$this->_dataSource=new TMap($dataSource);
		else if($dataSource instanceof Traversable)
			$this->_dataSource=$dataSource;
		else
			throw new TInvalidDataTypeException('readonlydatasourceview_datasource_invalid');
	}

	public function select($parameters)
	{
		return $this->_dataSource;
	}
}

