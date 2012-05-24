<?php
/**
 * IDataSource, TDataSourceControl, TReadOnlyDataSource class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDataSourceControl.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * IDataSource class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataSourceControl.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
interface IDataSource
{
	public function getView($viewName);
	public function getViewNames();
	public function onDataSourceChanged($param);
}

/**
 * TDataSourceControl class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataSourceControl.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
abstract class TDataSourceControl extends TControl implements IDataSource
{
	public function getView($viewName)
	{
		return null;
	}

	public function getViewNames()
	{
		return array();
	}

	public function onDataSourceChanged($param)
	{
		$this->raiseEvent('OnDataSourceChanged',$this,$param);
	}

	public function focus()
	{
		throw new TNotSupportedException('datasourcecontrol_focus_unsupported');
	}

	public function getEnableTheming()
	{
		return false;
	}

	public function setEnableTheming($value)
	{
		throw new TNotSupportedException('datasourcecontrol_enabletheming_unsupported');
	}

	public function getSkinID()
	{
		return '';
	}

	public function setSkinID($value)
	{
		throw new TNotSupportedException('datasourcecontrol_skinid_unsupported');
	}

	public function getVisible($checkParents=true)
	{
		return false;
	}

	public function setVisible($value)
	{
		throw new TNotSupportedException('datasourcecontrol_visible_unsupported');
	}
}

/**
 * TDataSourceControl class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataSourceControl.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TReadOnlyDataSource extends TDataSourceControl
{
	private $_dataSource;
	private $_dataMember;

	public function __construct($dataSource,$dataMember)
	{
		if(!is_array($dataSource) && !($dataSource instanceof IDataSource) && !($dataSource instanceof Traversable))
			throw new TInvalidDataTypeException('readonlydatasource_datasource_invalid');
		$this->_dataSource=$dataSource;
		$this->_dataMember=$dataMember;
	}

	public function getView($viewName)
	{
		if($this->_dataSource instanceof IDataSource)
			return $this->_dataSource->getView($viewName);
		else
			return new TReadOnlyDataSourceView($this,$this->_dataMember,$this->_dataSource);
	}
}

