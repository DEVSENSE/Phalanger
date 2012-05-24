<?php
/**
 * THyperLinkColumn class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: THyperLinkColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TDataGridColumn class file
 */
Prado::using('System.Web.UI.WebControls.TDataGridColumn');
/**
 * THyperLink class file
 */
Prado::using('System.Web.UI.WebControls.THyperLink');

/**
 * THyperLinkColumn class
 *
 * THyperLinkColumn contains a hyperlink for each item in the column.
 * You can set the text and the url of the hyperlink by {@link setText Text}
 * and {@link setNavigateUrl NavigateUrl} properties, respectively.
 * You can also bind the text and url to specific data field in datasource
 * by setting {@link setDataTextField DataTextField} and
 * {@link setDataNavigateUrlField DataNavigateUrlField}.
 * Both can be formatted before rendering according to the
 * {@link setDataTextFormatString DataTextFormatString} and
 * and {@link setDataNavigateUrlFormatString DataNavigateUrlFormatString}
 * properties, respectively. If both {@link setText Text} and {@link setDataTextField DataTextField}
 * are present, the latter takes precedence.
 * The same rule applies to {@link setNavigateUrl NavigateUrl} and
 * {@link setDataNavigateUrlField DataNavigateUrlField} properties.
 *
 * The hyperlinks in the column can be accessed by one of the following two methods:
 * <code>
 * $datagridItem->HyperLinkColumnID->HyperLink
 * $datagridItem->HyperLinkColumnID->Controls[0]
 * </code>
 * The second method is possible because the hyperlink control created within the
 * datagrid cell is the first child.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THyperLinkColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class THyperLinkColumn extends TDataGridColumn
{
	/**
	 * @return string the text caption of the hyperlink
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * Sets the text caption of the hyperlink.
	 * @param string the text caption to be set
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value,'');
	}

	/**
	 * @return string the field name from the data source to bind to the hyperlink caption
	 */
	public function getDataTextField()
	{
		return $this->getViewState('DataTextField','');
	}

	/**
	 * @param string the field name from the data source to bind to the hyperlink caption
	 */
	public function setDataTextField($value)
	{
		$this->setViewState('DataTextField',$value,'');
	}

	/**
	 * @return string the formatting string used to control how the hyperlink caption will be displayed.
	 */
	public function getDataTextFormatString()
	{
		return $this->getViewState('DataTextFormatString','');
	}

	/**
	 * @param string the formatting string used to control how the hyperlink caption will be displayed.
	 */
	public function setDataTextFormatString($value)
	{
		$this->setViewState('DataTextFormatString',$value,'');
	}

	/**
	 * @return string the URL to link to when the hyperlink is clicked.
	 */
	public function getNavigateUrl()
	{
		return $this->getViewState('NavigateUrl','');
	}

	/**
	 * Sets the URL to link to when the hyperlink is clicked.
	 * @param string the URL
	 */
	public function setNavigateUrl($value)
	{
		$this->setViewState('NavigateUrl',$value,'');
	}

	/**
	 * @return string the field name from the data source to bind to the navigate url of hyperlink
	 */
	public function getDataNavigateUrlField()
	{
		return $this->getViewState('DataNavigateUrlField','');
	}

	/**
	 * @param string the field name from the data source to bind to the navigate url of hyperlink
	 */
	public function setDataNavigateUrlField($value)
	{
		$this->setViewState('DataNavigateUrlField',$value,'');
	}

	/**
	 * @return string the formatting string used to control how the navigate url of hyperlink will be displayed.
	 */
	public function getDataNavigateUrlFormatString()
	{
		return $this->getViewState('DataNavigateUrlFormatString','');
	}

	/**
	 * @param string the formatting string used to control how the navigate url of hyperlink will be displayed.
	 */
	public function setDataNavigateUrlFormatString($value)
	{
		$this->setViewState('DataNavigateUrlFormatString',$value,'');
	}

	/**
	 * @return string the target window or frame to display the Web page content linked to when the hyperlink is clicked.
	 */
	public function getTarget()
	{
		return $this->getViewState('Target','');
	}

	/**
	 * Sets the target window or frame to display the Web page content linked to when the hyperlink is clicked.
	 * @param string the target window, valid values include '_blank', '_parent', '_self', '_top' and empty string.
	 */
	public function setTarget($value)
	{
		$this->setViewState('Target',$value,'');
	}

	/**
	 * Initializes the specified cell to its initial values.
	 * This method overrides the parent implementation.
	 * It creates a hyperlink within the cell.
	 * @param TTableCell the cell to be initialized.
	 * @param integer the index to the Columns property that the cell resides in.
	 * @param string the type of cell (Header,Footer,Item,AlternatingItem,EditItem,SelectedItem)
	 */
	public function initializeCell($cell,$columnIndex,$itemType)
	{
		if($itemType===TListItemType::Item || $itemType===TListItemType::AlternatingItem || $itemType===TListItemType::SelectedItem || $itemType===TListItemType::EditItem)
		{
			$link=new THyperLink;
			$link->setText($this->getText());
			$link->setNavigateUrl($this->getNavigateUrl());
			$link->setTarget($this->getTarget());
			if($this->getDataTextField()!=='' || $this->getDataNavigateUrlField()!=='')
				$link->attachEventHandler('OnDataBinding',array($this,'dataBindColumn'));
			$cell->getControls()->add($link);
			$cell->registerObject('HyperLink',$link);
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
		if(($field=$this->getDataTextField())!=='')
		{
			$value=$this->getDataFieldValue($data,$field);
			$text=$this->formatDataValue($this->getDataTextFormatString(),$value);
			$sender->setText($text);
		}
		if(($field=$this->getDataNavigateUrlField())!=='')
		{
			$value=$this->getDataFieldValue($data,$field);
			$url=$this->formatDataValue($this->getDataNavigateUrlFormatString(),$value);
			$sender->setNavigateUrl($url);
		}
	}
}

