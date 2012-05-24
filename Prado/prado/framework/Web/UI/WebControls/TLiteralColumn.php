<?php
/**
 * TLiteralColumn class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TLiteralColumn.php 1397 2006-09-07 07:55:53Z wei $
 * @package System.Web.UI.WebControls
 */

/**
 * TDataGridColumn class file
 */
Prado::using('System.Web.UI.WebControls.TDataGridColumn');

/**
 * TLiteralColumn class
 *
 * TLiteralColumn represents a static text column that is bound to a field in a data source.
 * The cells in the column will be displayed with static texts using the data indexed by
 * {@link setDataField DataField}. You can customize the display by
 * setting {@link setDataFormatString DataFormatString}.
 *
 * If {@link setDataField DataField} is not specified, the cells will be filled
 * with {@link setText Text}.
 *
 * If {@link setEncode Encode} is true, the static texts will be HTML-encoded.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLiteralColumn.php 1397 2006-09-07 07:55:53Z wei $
 * @package System.Web.UI.WebControls
 * @since 3.0.5
 */
class TLiteralColumn extends TDataGridColumn
{
	/**
	 * @return string the field name from the data source to bind to the column
	 */
	public function getDataField()
	{
		return $this->getViewState('DataField','');
	}

	/**
	 * @param string the field name from the data source to bind to the column
	 */
	public function setDataField($value)
	{
		$this->setViewState('DataField',$value,'');
	}

	/**
	 * @return string the formatting string used to control how the bound data will be displayed.
	 */
	public function getDataFormatString()
	{
		return $this->getViewState('DataFormatString','');
	}

	/**
	 * @param string the formatting string used to control how the bound data will be displayed.
	 */
	public function setDataFormatString($value)
	{
		$this->setViewState('DataFormatString',$value,'');
	}

	/**
	 * @return string static text to be displayed in the column. Defaults to empty.
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * @param string static text to be displayed in the column.
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value,'');
	}

	/**
	 * @return boolean whether the rendered text should be HTML-encoded. Defaults to false.
	 */
	public function getEncode()
	{
		return $this->getViewState('Encode',false);
	}

	/**
	 * @param boolean  whether the rendered text should be HTML-encoded.
	 */
	public function setEncode($value)
	{
		$this->setViewState('Encode',TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * Initializes the specified cell to its initial values.
	 * This method overrides the parent implementation.
	 * @param TTableCell the cell to be initialized.
	 * @param integer the index to the Columns property that the cell resides in.
	 * @param string the type of cell (Header,Footer,Item,AlternatingItem,EditItem,SelectedItem)
	 */
	public function initializeCell($cell,$columnIndex,$itemType)
	{
		if($itemType===TListItemType::Item || $itemType===TListItemType::AlternatingItem || $itemType===TListItemType::EditItem || $itemType===TListItemType::SelectedItem)
		{
			if($this->getDataField()!=='')
				$cell->attachEventHandler('OnDataBinding',array($this,'dataBindColumn'));
			else
			{
				if(($dataField=$this->getDataField())!=='')
					$control->attachEventHandler('OnDataBinding',array($this,'dataBindColumn'));
				else
				{
					$text=$this->getText();
					if($this->getEncode())
						$text=THttpUtility::htmlEncode($text);
					$cell->setText($text);
				}
			}
		}
		else
			parent::initializeCell($cell,$columnIndex,$itemType);
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
		$formatString=$this->getDataFormatString();
		if(($field=$this->getDataField())!=='')
			$value=$this->formatDataValue($formatString,$this->getDataFieldValue($data,$field));
		else
			$value=$this->formatDataValue($formatString,$data);
		if($sender instanceof TTableCell)
		{
			if($this->getEncode())
				$value=THttpUtility::htmlEncode($value);
			$sender->setText($value);
		}
	}
}

