<?php
/**
 * TDropDownListColumn class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDropDownListColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.Web.UI.WebControls.TDataGridColumn');
Prado::using('System.Web.UI.WebControls.TDropDownList');

/**
 * TDropDownListColumn class
 *
 * TDropDownListColumn represents a column that is bound to a field in a data source.
 * The cells in the column will be displayed using the data indexed by
 * {@link setDataTextField DataTextField}. You can customize the display by
 * setting {@link setDataTextFormatString DataTextFormatString}.
 *
 * If {@link setReadOnly ReadOnly} is false, TDropDownListColumn will display cells in edit mode
 * with dropdown lists. Otherwise, a static text is displayed.
 * The currently selected dropndown list item is specified by the data indexed with
 * {@link setDataValueField DataValueField}.
 *
 * There are two approaches to specify the list items available for selection.
 * The first approach uses template syntax as follows,
 * <code>
 *   <com:TDropDownListColumn ....>
 *     <com:TListItem Value="1" Text="first item" />
 *     <com:TListItem Value="2" Text="second item" />
 *     <com:TListItem Value="3" Text="third item" />
 *   </com:TDropDownListColumn>
 * </code>
 * The second approach specifies a data source to be bound to the dropdown lists
 * by setting {@link setListDataSource ListDataSource}. Like generic list controls,
 * you may also want to specify which data fields are used for item values and texts
 * by setting {@link setListValueField ListValueField} and
 * {@link setListTextField ListTextField}, respectively.
 * Furthermore, the item texts may be formatted by using {@link setListTextFormatString ListTextFormatString}.
 * Note, if you specify {@link setListDataSource ListDataSource}, do it before
 * calling the datagrid's dataBind().
 *
 * The dropdown list control in the TDropDownListColumn can be accessed by one of
 * the following two methods:
 * <code>
 * $datagridItem->DropDownListColumnID->DropDownList
 * $datagridItem->DropDownListColumnID->Controls[0]
 * </code>
 * The second method is possible because the dropdown list control created within the
 * datagrid cell is the first child.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDropDownListColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TDropDownListColumn extends TDataGridColumn
{
	private $_stateLoaded=false;
	private $_dataBound=false;
	private $_listControl=null;

	public function __construct()
	{
		$this->_listControl=new TDropDownList;
	}

	/**
	 * Loads items from viewstate.
	 * This method overrides the parent implementation by loading list items
	 * @param mixed state values
	 */
	public function loadState($state)
	{
		parent::loadState($state);
		$this->_stateLoaded=true;
		if(!$this->_dataBound)
			$this->_listControl->getItems()->loadState($this->getViewState('Items',null));
	}

	/**
	 * Saves items into viewstate.
	 * This method overrides the parent implementation by saving list items
	 */
	public function saveState()
	{
		$this->setViewState('Items',$this->_listControl->getItems()->saveState(),null);
		return parent::saveState();
	}

	/**
	 * Adds object parsed from template to the control.
	 * This method adds only {@link TListItem} objects into the {@link getItems Items} collection.
	 * All other objects are ignored.
	 * @param mixed object parsed from template
	 */
	public function addParsedObject($object)
	{
		// Do not add items from template if items are loaded from viewstate
		if(!$this->_stateLoaded && ($object instanceof TListItem))
		{
			$object->setSelected(false);
			$index=$this->_listControl->getItems()->add($object);
		}
	}

	/**
	 * @return string the field of the data source that provides the text content of the column.
	 */
	public function getDataTextField()
	{
		return $this->getViewState('DataTextField','');
	}

	/**
	 * Sets the field of the data source that provides the text content of the column.
	 * If this is not set, the data specified via {@link getDataValueField DataValueField}
	 * will be displayed in the column.
	 * @param string the field of the data source that provides the text content of the column.
	 */
	public function setDataTextField($value)
	{
		$this->setViewState('DataTextField',$value,'');
	}

	/**
	 * @return string the formatting string used to control how the bound data will be displayed.
	 */
	public function getDataTextFormatString()
	{
		return $this->getViewState('DataTextFormatString','');
	}

	/**
	 * @param string the formatting string used to control how the bound data will be displayed.
	 */
	public function setDataTextFormatString($value)
	{
		$this->setViewState('DataTextFormatString',$value,'');
	}

	/**
	 * @return string the field of the data source that provides the key selecting an item in dropdown list.
	 */
	public function getDataValueField()
	{
		return $this->getViewState('DataValueField','');
	}

	/**
	 * Sets the field of the data source that provides the key selecting an item in dropdown list.
	 * If this is not present, the data specified via {@link getDataTextField DataTextField} (without
	 * applying the formatting string) will be used for selection, instead.
	 * @param string the field of the data source that provides the key selecting an item in dropdown list.
	 */
	public function setDataValueField($value)
	{
		$this->setViewState('DataValueField',$value,'');
	}

	/**
	 * @return boolean whether the items in the column can be edited. Defaults to false.
	 */
	public function getReadOnly()
	{
		return $this->getViewState('ReadOnly',false);
	}

	/**
	 * @param boolean whether the items in the column can be edited
	 */
	public function setReadOnly($value)
	{
		$this->setViewState('ReadOnly',TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * @return Traversable data source to be bound to the dropdown list boxes.
	 */
	public function getListDataSource()
	{
		return $this->_listControl->getDataSource();
	}

	/**
	 * @param Traversable|array|string data source to be bound to the dropdown list boxes.
	 */
	public function setListDataSource($value)
	{
		$this->_listControl->setDataSource($value);
	}

	/**
	 * @return string the data field used to populate the values of the dropdown list items. Defaults to empty.
	 */
	public function getListValueField()
	{
		return $this->getViewState('ListValueField','');
	}

	/**
	 * @param string the data field used to populate the values of the dropdown list items
	 */
	public function setListValueField($value)
	{
		$this->setViewState('ListValueField',$value,'');
	}

	/**
	 * @return string the data field used to populate the texts of the dropdown list items. Defaults to empty.
	 */
	public function getListTextField()
	{
		return $this->getViewState('ListTextField','');
	}

	/**
	 * @param string the data field used to populate the texts of the dropdown list items
	 */
	public function setListTextField($value)
	{
		$this->setViewState('ListTextField',$value,'');
	}

	/**
	 * @return string the formatting string used to control how the list item texts will be displayed.
	 */
	public function getListTextFormatString()
	{
		return $this->getViewState('ListTextFormatString','');
	}

	/**
	 * @param string the formatting string used to control how the list item texts will be displayed.
	 */
	public function setListTextFormatString($value)
	{
		$this->setViewState('ListTextFormatString',$value,'');
	}

	/**
	 * Initializes the specified cell to its initial values.
	 * This method overrides the parent implementation.
	 * It creates a textbox for item in edit mode and the column is not read-only.
	 * Otherwise it displays a static text.
	 * The caption of the button and the static text are retrieved
	 * from the datasource.
	 * @param TTableCell the cell to be initialized.
	 * @param integer the index to the Columns property that the cell resides in.
	 * @param string the type of cell (Header,Footer,Item,AlternatingItem,EditItem,SelectedItem)
	 */
	public function initializeCell($cell,$columnIndex,$itemType)
	{
		if(!$this->_dataBound && $this->_listControl->getDataSource()!==null)
		{
			$this->_listControl->setDataTextField($this->getListTextField());
			$this->_listControl->setDataValueField($this->getListValueField());
			$this->_listControl->setDataTextFormatString($this->getListTextFormatString());
			$this->_listControl->dataBind();
			$this->_dataBound=true;
		}
		switch($itemType)
		{
			case TListItemType::EditItem:
				if(!$this->getReadOnly())
				{
					$listControl=clone $this->_listControl;
					$cell->getControls()->add($listControl);
					$cell->registerObject('DropDownList',$listControl);
					$control=$listControl;
				}
				else
					$control=$cell;
				$control->attachEventHandler('OnDataBinding',array($this,'dataBindColumn'));
				break;
			case TListItemType::Item:
			case TListItemType::AlternatingItem:
			case TListItemType::SelectedItem:
				if($this->getDataTextField()!=='' || $this->getDataValueField()!=='')
					$cell->attachEventHandler('OnDataBinding',array($this,'dataBindColumn'));
				break;
			default:
				parent::initializeCell($cell,$columnIndex,$itemType);
				break;
		}
	}

	/**
	 * Databinds a cell in the column.
	 * This method is invoked when datagrid performs databinding.
	 * It populates the content of the cell with the relevant data from data source.
	 */
	public function dataBindColumn($sender,$param)
	{
		$item=$sender->getNamingContainer();
		$data=$item->getData();
		if(($valueField=$this->getDataValueField())!=='')
			$value=$this->getDataFieldValue($data,$valueField);
		else
			$value='';
		if(($textField=$this->getDataTextField())!=='')
		{
			$text=$this->getDataFieldValue($data,$textField);
			if($valueField==='')
				$value=$text;
			$formatString=$this->getDataTextFormatString();
			$text=$this->formatDataValue($formatString,$text);
		}
		else
			$text=$value;
		if($sender instanceof TTableCell)
			$sender->setText($text);
		else if($sender instanceof TDropDownList)
			$sender->setSelectedValue($value);
	}
}

