<?php
/**
 * TScaffoldBase class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TScaffoldBase.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.ActiveRecord.Scaffold
 */

/**
 * Include the base Active Record class.
 */
Prado::using('System.Data.ActiveRecord.TActiveRecord');

/**
 * Base class for Active Record scaffold views.
 *
 * Provides common properties for all scaffold views (such as, TScaffoldListView,
 * TScaffoldEditView, TScaffoldListView and TScaffoldView).
 *
 * During the OnPrRender stage the default css style file (filename style.css)
 * is published and registered. To override the default style, provide your own stylesheet
 * file explicitly.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TScaffoldBase.php 2880 2011-01-19 14:56:01Z christophe.boulain $
 * @package System.Data.ActiveRecord.Scaffold
 * @since 3.1
 */
abstract class TScaffoldBase extends TTemplateControl
{
	/**
	 * @var TActiveRecord record instance (may be new or retrieved from db)
	 */
	private $_record;

	/**
	 * @return TDbMetaData table/view information
	 */
	protected function getTableInfo()
	{
		$finder = $this->getRecordFinder();
		$gateway = $finder->getRecordManager()->getRecordGateWay();
		return $gateway->getRecordTableInfo($finder);
	}

	/**
	 * @param TActiveRecord record instance
	 * @return array record property values
	 */
	protected function getRecordPropertyValues($record)
	{
		$data = array();
		foreach($this->getTableInfo()->getColumns() as $name=>$column)
			$data[] = $record->getColumnValue($name);
		return $data;
	}

	/**
	 * @param TActiveRecord record instance
	 * @return array record primary key values.
	 */
	protected function getRecordPkValues($record)
	{
		$data=array();
		foreach($this->getTableInfo()->getColumns() as $name=>$column)
		{
			if($column->getIsPrimaryKey())
				$data[] = $record->getColumnValue($name);
		}
		return $data;
	}

	/**
	 * Name of the Active Record class to be viewed or scaffolded.
	 * @return string Active Record class name.
	 */
	public function getRecordClass()
	{
		return $this->getViewState('RecordClass');
	}

	/**
	 * Name of the Active Record class to be viewed or scaffolded.
	 * @param string Active Record class name.
	 */
	public function setRecordClass($value)
	{
		$this->setViewState('RecordClass', $value);
	}

	/**
	 * Copy the view details from another scaffold view instance.
	 * @param TScaffoldBase scaffold view.
	 */
	protected function copyFrom(TScaffoldBase $obj)
	{
		$this->_record = $obj->_record;
		$this->setRecordClass($obj->getRecordClass());
		$this->setEnableDefaultStyle($obj->getEnableDefaultStyle());
	}

	/**
	 * Unset the current record instance and table information.
	 */
	protected function clearRecordObject()
	{
		$this->_record=null;
	}

	/**
	 * Gets the current Active Record instance. Creates new instance if the
	 * primary key value is null otherwise the record is fetched from the db.
	 * @param array primary key value
	 * @return TActiveRecord record instance
	 */
	protected function getRecordObject($pk=null)
	{
		if($this->_record===null)
		{
			if($pk!==null)
			{
				$this->_record=$this->getRecordFinder()->findByPk($pk);
				if($this->_record===null)
					throw new TConfigurationException('scaffold_invalid_record_pk',
						$this->getRecordClass(), $pk);
			}
			else
			{
				$class = $this->getRecordClass();
				if($class!==null)
					$this->_record=Prado::createComponent($class);
				else
				{
					throw new TConfigurationException('scaffold_invalid_record_class',
						$this->getRecordClass(),$this->getID());
				}
			}
		}
		return $this->_record;
	}

	/**
	 * @param TActiveRecord Active Record instance.
	 */
	protected function setRecordObject(TActiveRecord $value)
	{
		$this->_record=$value;
	}

	/**
	 * @return TActiveRecord Active Record finder instance
	 */
	protected function getRecordFinder()
	{
		return TActiveRecord::finder($this->getRecordClass());
	}

	/**
	 * @return string default scaffold stylesheet name
	 */
	public function getDefaultStyle()
	{
		return $this->getViewState('DefaultStyle', 'style');
	}

	/**
	 * @param string default scaffold stylesheet name
	 */
	public function setDefaultStyle($value)
	{
		$this->setViewState('DefaultStyle', TPropertyValue::ensureString($value), 'style');
	}

	/**
	 * @return boolean enable default stylesheet, default is true.
	 */
	public function getEnableDefaultStyle()
	{
		return $this->getViewState('EnableDefaultStyle', true);
	}

	/**
	 * @param boolean enable default stylesheet, default is true.
	 */
	public function setEnableDefaultStyle($value)
	{
		return $this->setViewState('EnableDefaultStyle', TPropertyValue::ensureBoolean($value), true);
	}

	/**
	 * Publish the default stylesheet file.
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		if($this->getEnableDefaultStyle())
		{
			$url = $this->publishAsset($this->getDefaultStyle().'.css');
			$this->getPage()->getClientScript()->registerStyleSheetFile($url,$url);
		}
	}
}

