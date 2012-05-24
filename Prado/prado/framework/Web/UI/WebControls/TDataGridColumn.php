<?php
/**
 * TDataGridColumn class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDataGridColumn.php 2756 2010-01-14 13:12:27Z Christophe.Boulain $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.Util.TDataFieldAccessor');
Prado::using('System.Web.UI.WebControls.TDataGrid');

/**
 * TDataGridColumn class
 *
 * TDataGridColumn serves as the base class for the different column types of
 * the {@link TDataGrid} control.
 * TDataGridColumn defines the properties and methods that are common among
 * all datagrid column types. In particular, it initializes header and footer
 * cells according to {@link setHeaderText HeaderText} and {@link getHeaderStyle HeaderStyle}
 * {@link setFooterText FooterText} and {@link getFooterStyle FooterStyle} properties.
 * If {@link setHeaderImageUrl HeaderImageUrl} is specified, the image
 * will be displayed instead in the header cell.
 * The {@link getItemStyle ItemStyle} is applied to cells that belong to
 * non-header and -footer datagrid items.
 *
 * When the datagrid enables sorting, if the {@link setSortExpression SortExpression}
 * is not empty, the header cell will display a button (linkbutton or imagebutton)
 * that will bubble the sort command event to the datagrid.
 *
 * Since v3.1.0, TDataGridColumn has introduced two new properties {@link setHeaderRenderer HeaderRenderer}
 * and {@link setFooterRenderer FooterRenderer} which can be used to specify
 * the layout of header and footer column cells.
 * A renderer refers to a control class that is to be instantiated as a control.
 * For more details, see {@link TRepeater} and {@link TDataList}.
 *
 * Since v3.1.1, TDataGridColumn has introduced {@link setEnableCellGrouping EnableCellGrouping}.
 * If a column has this property set true, consecutive cells having the same content in this
 * column will be grouped into one cell.
 * Note, there are some limitations to cell grouping. We determine the cell content according to
 * the cell's {@link TTableCell::getText Text} property. If the text is empty and the cell has
 * some child controls, we will pick up the first control who implements {@link IDataRenderer}
 * and obtain its {@link IDataRenderer::getData Data} property.
 *
 * The following datagrid column types are provided by the framework currently,
 * - {@link TBoundColumn}, associated with a specific field in datasource and displays the corresponding data.
 * - {@link TEditCommandColumn}, displaying edit/update/cancel command buttons
 * - {@link TDropDownListColumn}, displaying a dropdown list when the item is in edit state
 * - {@link TButtonColumn}, displaying generic command buttons that may be bound to specific field in datasource.
 * - {@link THyperLinkColumn}, displaying a hyperlink that may be bound to specific field in datasource.
 * - {@link TCheckBoxColumn}, displaying a checkbox that may be bound to specific field in datasource.
 * - {@link TTemplateColumn}, displaying content based on templates.
 *
 * To create your own column class, simply override {@link initializeCell()} method,
 * which is the major logic for managing the data and presentation of cells in the column.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataGridColumn.php 2756 2010-01-14 13:12:27Z Christophe.Boulain $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
abstract class TDataGridColumn extends TApplicationComponent
{
	private $_id='';
	private $_owner=null;
	private $_viewState=array();

	/**
	 * @return string the ID of the column.
	 */
	public function getID()
	{
		return $this->_id;
	}

	/**
	 * Sets the ID of the column.
	 * By explicitly specifying the column ID, one can access the column
	 * by $templateControl->ColumnID.
	 * @param string the ID of the column.
	 * @throws TInvalidDataValueException if the ID is of bad format
	 */
	public function setID($value)
	{
		if(!preg_match(TControl::ID_FORMAT,$value))
			throw new TInvalidDataValueException('datagridcolumn_id_invalid',get_class($this),$value);
		$this->_id=$value;
	}

	/**
	 * @return string the text to be displayed in the header of this column
	 */
	public function getHeaderText()
	{
		return $this->getViewState('HeaderText','');
	}

	/**
	 * @param string text to be displayed in the header of this column
	 */
	public function setHeaderText($value)
	{
		$this->setViewState('HeaderText',$value,'');
	}

	/**
	 * @return string the url of the image to be displayed in header
	 */
	public function getHeaderImageUrl()
	{
		return $this->getViewState('HeaderImageUrl','');
	}

	/**
	 * @param string the url of the image to be displayed in header
	 */
	public function setHeaderImageUrl($value)
	{
		$this->setViewState('HeaderImageUrl',$value,'');
	}

	/**
	 * @return string the class name for the column header cell renderer. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getHeaderRenderer()
	{
		return $this->getViewState('HeaderRenderer','');
	}

	/**
	 * Sets the column header cell renderer class.
	 *
	 * If not empty, the class will be used to instantiate as a child control in the column header cell.
	 * If the class implements {@link IDataRenderer}, the <b>Data</b> property
	 * will be set as the {@link getFooterText FooterText}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @since 3.1.0
	 */
	public function setHeaderRenderer($value)
	{
		$this->setViewState('HeaderRenderer',$value,'');
	}

	/**
	 * @param boolean whether to create a style if previously not existing
	 * @return TTableItemStyle the style for header
	 */
	public function getHeaderStyle($createStyle=true)
	{
		if(($style=$this->getViewState('HeaderStyle',null))===null && $createStyle)
		{
			$style=new TTableItemStyle;
			$this->setViewState('HeaderStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return string the text to be displayed in the footer of this column
	 */
	public function getFooterText()
	{
		return $this->getViewState('FooterText','');
	}

	/**
	 * @param string text to be displayed in the footer of this column
	 */
	public function setFooterText($value)
	{
		$this->setViewState('FooterText',$value,'');
	}

	/**
	 * @return string the class name for the column footer cell renderer. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getFooterRenderer()
	{
		return $this->getViewState('FooterRenderer','');
	}

	/**
	 * Sets the column footer cell renderer class.
	 *
	 * If not empty, the class will be used to instantiate as a child control in the column footer cell.
	 * If the class implements {@link IDataRenderer}, the <b>Data</b> property
	 * will be set as the {@link getFooterText FooterText}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @since 3.1.0
	 */
	public function setFooterRenderer($value)
	{
		$this->setViewState('FooterRenderer',$value,'');
	}

	/**
	 * @param boolean whether to create a style if previously not existing
	 * @return TTableItemStyle the style for footer
	 */
	public function getFooterStyle($createStyle=true)
	{
		if(($style=$this->getViewState('FooterStyle',null))===null && $createStyle)
		{
			$style=new TTableItemStyle;
			$this->setViewState('FooterStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @param boolean whether to create a style if previously not existing
	 * @return TTableItemStyle the style for item
	 */
	public function getItemStyle($createStyle=true)
	{
		if(($style=$this->getViewState('ItemStyle',null))===null && $createStyle)
		{
			$style=new TTableItemStyle;
			$this->setViewState('ItemStyle',$style,null);
		}
		return $style;
	}

	/**
	 * @return string the name of the field or expression for sorting
	 */
	public function getSortExpression()
	{
		return $this->getViewState('SortExpression','');
	}

	/**
	 * @param string the name of the field or expression for sorting
	 */
	public function setSortExpression($value)
	{
		$this->setViewState('SortExpression',$value,'');
	}

	/**
	 * @return boolean whether cells having the same content should be grouped together. Defaults to false.
	 * @since 3.1.1
	 */
	public function getEnableCellGrouping()
	{
		return $this->getViewState('EnableCellGrouping',false);
	}

	/**
	 * @param boolean whether cells having the same content should be grouped together.
	 * @since 3.1.1
	 */
	public function setEnableCellGrouping($value)
	{
		$this->setViewState('EnableCellGrouping',TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * @return boolean whether the column is visible. Defaults to true.
	 */
	public function getVisible($checkParents=true)
	{
		return $this->getViewState('Visible',true);
	}

	/**
	 * @param boolean whether the column is visible
	 */
	public function setVisible($value)
	{
		$this->setViewState('Visible',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * Returns a viewstate value.
	 *
	 * @param string the name of the viewstate value to be returned
	 * @param mixed the default value. If $key is not found in viewstate, $defaultValue will be returned
	 * @return mixed the viewstate value corresponding to $key
	 */
	protected function getViewState($key,$defaultValue=null)
	{
		return isset($this->_viewState[$key])?$this->_viewState[$key]:$defaultValue;
	}

	/**
	 * Sets a viewstate value.
	 *
	 * Make sure that the viewstate value must be serializable and unserializable.
	 * @param string the name of the viewstate value
	 * @param mixed the viewstate value to be set
	 * @param mixed default value. If $value===$defaultValue, the item will be cleared from the viewstate.
	 */
	protected function setViewState($key,$value,$defaultValue=null)
	{
		if($value===$defaultValue)
			unset($this->_viewState[$key]);
		else
			$this->_viewState[$key]=$value;
	}

	/**
	 * Loads persistent state values.
	 * @param mixed state values
	 */
	public function loadState($state)
	{
		$this->_viewState=$state;
	}

	/**
	 * Saves persistent state values.
	 * @return mixed values to be saved
	 */
	public function saveState()
	{
		return $this->_viewState;
	}

	/**
	 * @return TDataGrid datagrid that owns this column
	 */
	public function getOwner()
	{
		return $this->_owner;
	}

	/**
	 * @param TDataGrid datagrid object that owns this column
	 */
	public function setOwner(TDataGrid $value)
	{
		$this->_owner=$value;
	}

	/**
	 * Initializes the column.
	 * This method is invoked by {@link TDataGrid} when the column
	 * is about to be used to initialize datagrid items.
	 * Derived classes may override this method to do additional initialization.
	 */
	public function initialize()
	{
	}

	/**
	 * Fetches the value of the data at the specified field.
	 * If the data is an array, the field is used as an array key.
	 * If the data is an of {@link TMap}, {@link TList} or their derived class,
	 * the field is used as a key value.
	 * If the data is a component, the field is used as the name of a property.
	 * @param mixed data containing the field of value
	 * @param string the data field
	 * @return mixed data value at the specified field
	 * @throws TInvalidDataValueException if the data or the field is invalid.
	 */
	protected function getDataFieldValue($data,$field)
	{
		return TDataFieldAccessor::getDataFieldValue($data,$field);
	}


	/**
	 * Initializes the specified cell to its initial values.
	 * The default implementation sets the content of header and footer cells.
	 * If sorting is enabled by the grid and sort expression is specified in the column,
	 * the header cell will show a link/image button. Otherwise, the header/footer cell
	 * will only show static text/image.
	 * This method can be overriden to provide customized intialization to column cells.
	 * @param TTableCell the cell to be initialized.
	 * @param integer the index to the Columns property that the cell resides in.
	 * @param string the type of cell (Header,Footer,Item,AlternatingItem,EditItem,SelectedItem)
	 */
	public function initializeCell($cell,$columnIndex,$itemType)
	{
		if($itemType===TListItemType::Header)
			$this->initializeHeaderCell($cell,$columnIndex);
		else if($itemType===TListItemType::Footer)
			$this->initializeFooterCell($cell,$columnIndex);
	}

	/**
	 * Returns a value indicating whether this column allows sorting.
	 * The column allows sorting only when {@link getSortExpression SortExpression}
	 * is not empty and the datagrid allows sorting.
	 * @return boolean whether this column allows sorting
	 */
	public function getAllowSorting()
	{
		return $this->getSortExpression()!=='' && (!$this->_owner || $this->_owner->getAllowSorting());
	}

	/**
	 * Initializes the header cell.
	 *
	 * This method attempts to use {@link getHeaderRenderer HeaderRenderer} to
	 * instantiate the header cell. If that is not available, it will populate
	 * the cell with an image or a text string, depending on {@link getHeaderImageUrl HeaderImageUrl}
	 * and {@link getHeaderText HeaderText} property values.
	 *
	 * If the column allows sorting, image or text will be created as
	 * a button which issues <b>Sort</b> command upon user click.
	 *
	 * @param TTableCell the cell to be initialized
	 * @param integer the index to the Columns property that the cell resides in.
	 */
	protected function initializeHeaderCell($cell,$columnIndex)
	{
		$text=$this->getHeaderText();

		if(($classPath=$this->getHeaderRenderer())!=='')
		{
			$control=Prado::createComponent($classPath);
			$cell->getControls()->add($control);
			if($control instanceof IDataRenderer)
			{
				if($control instanceof IItemDataRenderer)
				{
					$item=$cell->getParent();
					$control->setItemIndex($item->getItemIndex());
					$control->setItemType($item->getItemType());
				}
				$control->setData($text);
			}
		}
		else if($this->getAllowSorting())
		{
			$sortExpression=$this->getSortExpression();
			if(($url=$this->getHeaderImageUrl())!=='')
			{
				$button=Prado::createComponent('System.Web.UI.WebControls.TImageButton');
				$button->setImageUrl($url);
				$button->setCommandName(TDataGrid::CMD_SORT);
				$button->setCommandParameter($sortExpression);
				if($text!=='')
					$button->setAlternateText($text);
				$button->setCausesValidation(false);
				$cell->getControls()->add($button);
			}
			else if($text!=='')
			{
				$button=Prado::createComponent('System.Web.UI.WebControls.TLinkButton');
				$button->setText($text);
				$button->setCommandName(TDataGrid::CMD_SORT);
				$button->setCommandParameter($sortExpression);
				$button->setCausesValidation(false);
				$cell->getControls()->add($button);
			}
			else
				$cell->setText('&nbsp;');
		}
		else
		{
			if(($url=$this->getHeaderImageUrl())!=='')
			{
				$image=Prado::createComponent('System.Web.UI.WebControls.TImage');
				$image->setImageUrl($url);
				if($text!=='')
					$image->setAlternateText($text);
				$cell->getControls()->add($image);
			}
			else if($text!=='')
				$cell->setText($text);
			else
				$cell->setText('&nbsp;');
		}
	}

	/**
	 * Initializes the footer cell.
	 *
	 * This method attempts to use {@link getFooterRenderer FooterRenderer} to
	 * instantiate the footer cell. If that is not available, it will populate
	 * the cell with a text string specified by {@link getFooterImageUrl FooterImageUrl}
	 *
	 * @param TTableCell the cell to be initialized
	 * @param integer the index to the Columns property that the cell resides in.
	 */
	protected function initializeFooterCell($cell,$columnIndex)
	{
		$text=$this->getFooterText();
		if(($classPath=$this->getFooterRenderer())!=='')
		{
			$control=Prado::createComponent($classPath);
			$cell->getControls()->add($control);
			if($control instanceof IDataRenderer)
			{
				if($control instanceof IItemDataRenderer)
				{
					$item=$cell->getParent();
					$control->setItemIndex($item->getItemIndex());
					$control->setItemType($item->getItemType());
				}
				$control->setData($text);
			}
		}
		else if($text!=='')
			$cell->setText($text);
		else
			$cell->setText('&nbsp;');
	}

	/**
	 * Formats the text value according to a format string.
	 * If the format string is empty, the original value is converted into
	 * a string and returned.
	 * If the format string starts with '#', the string is treated as a PHP expression
	 * within which the token '{0}' is translated with the data value to be formated.
	 * Otherwise, the format string and the data value are passed
	 * as the first and second parameters in {@link sprintf}.
	 * @param string format string
	 * @param mixed the data to be formatted
	 * @return string the formatted result
	 */
	protected function formatDataValue($formatString,$value)
	{
		if($formatString==='')
			return TPropertyValue::ensureString($value);
		else if($formatString[0]==='#')
		{
			$expression=strtr(substr($formatString,1),array('{0}'=>'$value'));
			try
			{
				if(eval("\$result=$expression;")===false)
					throw new Exception('');
				return $result;
			}
			catch(Exception $e)
			{
				throw new TInvalidDataValueException('datagridcolumn_expression_invalid',get_class($this),$expression,$e->getMessage());
			}
		}
		else
			return sprintf($formatString,$value);
	}
}


/**
 * TButtonColumnType class.
 * TButtonColumnType defines the enumerable type for the possible types of buttons
 * that can be used in a {@link TButtonColumn}.
 *
 * The following enumerable values are defined:
 * - LinkButton: link buttons
 * - PushButton: form buttons
 * - ImageButton: image buttons
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataGridColumn.php 2756 2010-01-14 13:12:27Z Christophe.Boulain $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TButtonColumnType extends TEnumerable
{
	const LinkButton='LinkButton';
	const PushButton='PushButton';
	const ImageButton='ImageButton';
}

