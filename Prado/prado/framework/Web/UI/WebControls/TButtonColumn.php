<?php
/**
 * TButtonColumn class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TButtonColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TDataGridColumn class file
 */
Prado::using('System.Web.UI.WebControls.TDataGridColumn');
Prado::using('System.Web.UI.WebControls.TButton');
Prado::using('System.Web.UI.WebControls.TLinkButton');
Prado::using('System.Web.UI.WebControls.TImageButton');

/**
 * TButtonColumn class
 *
 * TButtonColumn contains a user-defined command button, such as Add or Remove,
 * that corresponds with each row in the column.
 *
 * The caption of the buttons in the column is determined by {@link setText Text}
 * and {@link setDataTextField DataTextField} properties. If both are present,
 * the latter takes precedence. The {@link setDataTextField DataTextField} property
 * refers to the name of the field in datasource whose value will be used as the button caption.
 * If {@link setDataTextFormatString DataTextFormatString} is not empty,
 * the value will be formatted before rendering.
 *
 * The buttons in the column can be set to display as hyperlinks or push buttons
 * by setting the {@link setButtonType ButtonType} property.
 * The {@link setCommandName CommandName} will assign its value to
 * all button's <b>CommandName</b> property. The datagrid will capture
 * the command event where you can write event handlers based on different command names.
 * The buttons' <b>CausesValidation</b> and <b>ValidationGroup</b> property values
 * are determined by the column's corresponding properties.
 *
 * The buttons in the column can be accessed by one of the following two methods:
 * <code>
 * $datagridItem->ButtonColumnID->Button
 * $datagridItem->ButtonColumnID->Controls[0]
 * </code>
 * The second method is possible because the button control created within the
 * datagrid cell is the first child.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TButtonColumn.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TButtonColumn extends TDataGridColumn
{
	/**
	 * @return string the text caption of the button
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * Sets the text caption of the button.
	 * @param string the text caption to be set
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value,'');
	}

	/**
	 * @return string the field name from the data source to bind to the button caption
	 */
	public function getDataTextField()
	{
		return $this->getViewState('DataTextField','');
	}

	/**
	 * @param string the field name from the data source to bind to the button caption
	 */
	public function setDataTextField($value)
	{
		$this->setViewState('DataTextField',$value,'');
	}

	/**
	 * @return string the formatting string used to control how the button caption will be displayed.
	 */
	public function getDataTextFormatString()
	{
		return $this->getViewState('DataTextFormatString','');
	}

	/**
	 * @param string the formatting string used to control how the button caption will be displayed.
	 */
	public function setDataTextFormatString($value)
	{
		$this->setViewState('DataTextFormatString',$value,'');
	}

	/**
	 * @return string the URL of the image file for image buttons
	 */
	public function getImageUrl()
	{
		return $this->getViewState('ImageUrl','');
	}

	/**
	 * @param string the URL of the image file for image buttons
	 */
	public function setImageUrl($value)
	{
		$this->setViewState('ImageUrl',$value,'');
	}

	/**
	 * @return string the field name from the data source to bind to the button image url
	 */
	public function getDataImageUrlField()
	{
		return $this->getViewState('DataImageUrlField','');
	}

	/**
	 * @param string the field name from the data source to bind to the button image url
	 */
	public function setDataImageUrlField($value)
	{
		$this->setViewState('DataImageUrlField',$value,'');
	}

	/**
	 * @return string the formatting string used to control how the button image url will be displayed.
	 */
	public function getDataImageUrlFormatString()
	{
		return $this->getViewState('DataImageUrlFormatString','');
	}

	/**
	 * @param string the formatting string used to control how the button image url will be displayed.
	 */
	public function setDataImageUrlFormatString($value)
	{
		$this->setViewState('DataImageUrlFormatString',$value,'');
	}

	/**
	 * @return TButtonColumnType the type of command button. Defaults to TButtonColumnType::LinkButton.
	 */
	public function getButtonType()
	{
		return $this->getViewState('ButtonType',TButtonColumnType::LinkButton);
	}

	/**
	 * @param TButtonColumnType the type of command button
	 */
	public function setButtonType($value)
	{
		$this->setViewState('ButtonType',TPropertyValue::ensureEnum($value,'TButtonColumnType'),TButtonColumnType::LinkButton);
	}

	/**
	 * @return string the command name associated with the <b>OnCommand</b> event.
	 */
	public function getCommandName()
	{
		return $this->getViewState('CommandName','');
	}

	/**
	 * Sets the command name associated with the <b>Command</b> event.
	 * @param string the text caption to be set
	 */
	public function setCommandName($value)
	{
		$this->setViewState('CommandName',$value,'');
	}

	/**
	 * @return boolean whether postback event trigger by this button will cause input validation, default is true
	 */
	public function getCausesValidation()
	{
		return $this->getViewState('CausesValidation',true);
	}

	/**
	 * @param boolean whether postback event trigger by this button will cause input validation
	 */
	public function setCausesValidation($value)
	{
		$this->setViewState('CausesValidation',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @return string the group of validators which the button causes validation upon postback
	 */
	public function getValidationGroup()
	{
		return $this->getViewState('ValidationGroup','');
	}

	/**
	 * @param string the group of validators which the button causes validation upon postback
	 */
	public function setValidationGroup($value)
	{
		$this->setViewState('ValidationGroup',$value,'');
	}

	/**
	 * Initializes the specified cell to its initial values.
	 * This method overrides the parent implementation.
	 * It creates a command button within the cell.
	 * @param TTableCell the cell to be initialized.
	 * @param integer the index to the Columns property that the cell resides in.
	 * @param string the type of cell (Header,Footer,Item,AlternatingItem,EditItem,SelectedItem)
	 */
	public function initializeCell($cell,$columnIndex,$itemType)
	{
		if($itemType===TListItemType::Item || $itemType===TListItemType::AlternatingItem || $itemType===TListItemType::SelectedItem || $itemType===TListItemType::EditItem)
		{
			$buttonType=$this->getButtonType();
			if($buttonType===TButtonColumnType::LinkButton)
				$button=new TLinkButton;
			else if($buttonType===TButtonColumnType::PushButton)
				$button=new TButton;
			else // image button
			{
				$button=new TImageButton;
				$button->setImageUrl($this->getImageUrl());
			}
			$button->setText($this->getText());
			$button->setCommandName($this->getCommandName());
			$button->setCausesValidation($this->getCausesValidation());
			$button->setValidationGroup($this->getValidationGroup());
			if($this->getDataTextField()!=='' || ($buttonType===TButtonColumnType::ImageButton && $this->getDataImageUrlField()!==''))
				$button->attachEventHandler('OnDataBinding',array($this,'dataBindColumn'));
			$cell->getControls()->add($button);
			$cell->registerObject('Button',$button);
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
		if($sender instanceof IButtonControl)
		{
			if(($field=$this->getDataTextField())!=='')
			{
				$value=$this->getDataFieldValue($sender->getNamingContainer()->getData(),$field);
				$text=$this->formatDataValue($this->getDataTextFormatString(),$value);
				$sender->setText($text);
			}
			if(($sender instanceof TImageButton) && ($field=$this->getDataImageUrlField())!=='')
			{
				$value=$this->getDataFieldValue($sender->getNamingContainer()->getData(),$field);
				$url=$this->formatDataValue($this->getDataImageUrlFormatString(),$value);
				$sender->setImageUrl($url);
			}
		}
	}
}

