<?php
/**
 * TActiveDataGrid class file
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @link http://www.landwehr-software.de/
 * @copyright Copyright &copy; 2009 LANDWEHR Computer und Software GmbH
 * @license http://www.pradosoft.com/license/
 * @package System.Web.UI.ActiveControls
 */

/**
 * Includes the following used classes
 */
Prado::using('System.Web.UI.ActiveControls.TActiveControlAdapter');
Prado::using('System.Web.UI.ActiveControls.TActiveLinkButton');
Prado::using('System.Web.UI.ActiveControls.TActiveImageButton');
Prado::using('System.Web.UI.ActiveControls.TActiveButton');
Prado::using('System.Web.UI.ActiveControls.TActiveImage');
Prado::using('System.Web.UI.ActiveControls.TActiveCheckBox');
Prado::using('System.Web.UI.WebControls.TDataGrid');
Prado::using('System.Web.UI.WebControls.TBoundColumn');
Prado::using('System.Web.UI.WebControls.TEditCommandColumn');
Prado::using('System.Web.UI.WebControls.TButtonColumn');
Prado::using('System.Web.UI.WebControls.THyperLinkColumn');
Prado::using('System.Web.UI.WebControls.TCheckBoxColumn');

/**
 * TActiveDataGrid class
 *
 * TActiveDataGrid represents a data bound and updatable grid control which is the
 * active counterpart to the original {@link TDataGrid} control.
 *
 * This component can be used in the same way as the regular datagrid, the only
 * difference is that the active datagrid uses callbacks instead of postbacks
 * for interaction.
 *
 * There are also active datagrid columns to work with the TActiveDataGrid, which are
 * - {@link TActiveBoundColumn}, the active counterpart to {@link TBoundColumn}.
 * - {@link TActiveLiteralColumn}, the active counterpart to {@link TLiteralColumn}.
 * - {@link TActiveCheckBoxColumn}, the active counterpart to {@link TCheckBoxColumn}.
 * - {@link TActiveDropDownListColumn}, the active counterpart to {@link TDropDownListColumn}.
 * - {@link TActiveHyperLinkColumn}, the active counterpart to {@link THyperLinkColumn}.
 * - {@link TActiveEditCommandColumn}, the active counterpart to {@link TEditCommandColumn}.
 * - {@link TActiveButtonColumn}, the active counterpart to {@link TButtonColumn}.
 * - {@link TActiveTemplateColumn}, the active counterpart to {@link TTemplateColumn}.
 *
 * Please refer to the original documentation of the regular counterparts for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveDataGrid extends TDataGrid implements IActiveControl, ISurroundable {
/**
 * Creates a new callback control, sets the adapter to
 * TActiveControlAdapter.
 */
	public function __construct() {
		parent::__construct();
		$this->setAdapter(new TActiveControlAdapter($this));
	}

	/**
	 * @return TBaseActiveControl standard active control options.
	 */
	public function getActiveControl() {
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * Sets the data source object associated with the datagrid control.
	 * In addition, the render method of all connected pagers is called so they
	 * get updated when the data source is changed. Also the datagrid registers
	 * itself for rendering in order to get it's content replaced on client side.
	 * @param Traversable|array|string data source object
	 */
	public function setDataSource($value) {
		parent::setDataSource($value);
		if($this->getActiveControl()->canUpdateClientSide()) {
			$this->renderPager();
			$this->getPage()->getAdapter()->registerControlToRender($this,$this->getResponse()->createHtmlWriter());
		}
	}

	/**
	 * Returns the id of the surrounding container (span).
	 * @return string container id
	 */
	public function getSurroundingTagId() {
		return $this->ClientID.'_Container';
	}

	/**
	 * Creates a pager button.
	 * Depending on the button type, a TActiveLinkButton or a TActiveButton may be created.
	 * If it is enabled (clickable), its command name and parameter will also be set.
	 * It overrides the datagrid's original method to create active controls instead, thus
	 * the pager will do callbacks instead of the regular postbacks.
	 * @param string button type, either LinkButton or PushButton
	 * @param boolean whether the button should be enabled
	 * @param string caption of the button
	 * @param string CommandName corresponding to the OnCommand event of the button
	 * @param string CommandParameter corresponding to the OnCommand event of the button
	 * @return mixed the button instance
	 */
	protected function createPagerButton($buttonType,$enabled,$text,$commandName,$commandParameter) {
		if($buttonType===TDataGridPagerButtonType::LinkButton) {
			if($enabled)
				$button=new TActiveLinkButton;
			else {
				$button=new TLabel;
				$button->setText($text);
				return $button;
			}
		}
		else {
			$button=new TActiveButton;
			if(!$enabled)
				$button->setEnabled(false);
		}
		$button->setText($text);
		$button->setCommandName($commandName);
		$button->setCommandParameter($commandParameter);
		$button->setCausesValidation(false);
		return $button;
	}

	/**
	 * Renders the datagrid.
	 * If the datagrid did not pass the prerender phase yet, it will register itself for rendering later.
	 * Else it will call the {@link renderDataGrid()} method which will do the rendering of the datagrid.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function render($writer) {
		if($this->getHasPreRendered()) {
			$this->renderDataGrid($writer);
			if($this->getActiveControl()->canUpdateClientSide()) $this->getPage()->getCallbackClient()->replaceContent($this->getSurroundingTagId(),$writer);
		}
		else {
			$this->getPage()->getAdapter()->registerControlToRender($this,$writer);
		}
	}

	/**
	 * Loops through all {@link TActivePager} on the page and registers the ones which are set to paginate
	 * the datagrid for rendering. This is to ensure that the connected pagers are also rendered if the
	 * data source changed.
	 */
	private function renderPager() {
		$pager=$this->getPage()->findControlsByType('TActivePager', false);
		foreach($pager as $item) {
			if($item->ControlToPaginate==$this->ID) {
				$writer=$this->getResponse()->createHtmlWriter();
				$this->getPage()->getAdapter()->registerControlToRender($item,$writer);
			}
		}
	}

	/**
	 * Renders the datagrid by writing a span tag with the container id obtained from {@link getSurroundingTagId()}
	 * which will be called by the replacement method of the client script to update it's content.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	private function renderDataGrid($writer) {
		$writer->write('<span id="'.$this->getSurroundingTagId().'">');
		parent::render($writer);
		$writer->write('</span>');
	}
}


/**
 * TActiveBoundColumn class
 *
 * TActiveBoundColumn represents a column that is bound to a field in a data source.
 * The cells in the column will be displayed using the data indexed by
 * {@link setDataField DataField}. You can customize the display by
 * setting {@link setDataFormatString DataFormatString}.
 *
 * This is the active counterpart to the {@link TBoundColumn} control. For that purpose,
 * if sorting is allowed, the header links/buttons are replaced by active controls.
 *
 * Please refer to the original documentation of the {@link TBoundColumn} for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveBoundColumn extends TBoundColumn {
	protected function initializeHeaderCell($cell,$columnIndex) {
		$text=$this->getHeaderText();

		if(($classPath=$this->getHeaderRenderer())!=='') {
			$control=Prado::createComponent($classPath);
			if($control instanceof IDataRenderer) {
				if($control instanceof IItemDataRenderer) {
					$item=$cell->getParent();
					$control->setItemIndex($item->getItemIndex());
					$control->setItemType($item->getItemType());
				}
				$control->setData($text);
			}
			$cell->getControls()->add($control);
		}
		else if($this->getAllowSorting()) {
				$sortExpression=$this->getSortExpression();
				if(($url=$this->getHeaderImageUrl())!=='') {
					$button=Prado::createComponent('System.Web.UI.WebControls.TActiveImageButton');
					$button->setImageUrl($url);
					$button->setCommandName(TDataGrid::CMD_SORT);
					$button->setCommandParameter($sortExpression);
					if($text!=='') {
						$button->setAlternateText($text);
						$button->setToolTip($text);
					}
					$button->setCausesValidation(false);
					$cell->getControls()->add($button);
				}
				else if($text!=='') {
						$button=Prado::createComponent('System.Web.UI.WebControls.TActiveLinkButton');
						$button->setText($text);
						$button->setCommandName(TDataGrid::CMD_SORT);
						$button->setCommandParameter($sortExpression);
						$button->setCausesValidation(false);
						$cell->getControls()->add($button);
					}
					else
						$cell->setText('&nbsp;');
			}
			else {
				if(($url=$this->getHeaderImageUrl())!=='') {
					$image=Prado::createComponent('System.Web.UI.WebControls.TActiveImage');
					$image->setImageUrl($url);
					if($text!=='') {
						$image->setAlternateText($text);
						$image->setToolTip($text);
					}
					$cell->getControls()->add($image);
				}
				else if($text!=='')
						$cell->setText($text);
					else
						$cell->setText('&nbsp;');
			}
	}
}


/**
 * TActiveEditCommandColumn class
 *
 * TActiveEditCommandColumn contains the Edit command buttons for editing data items in each row.
 *
 * TActiveEditCommandColumn will create an edit button if a cell is not in edit mode.
 * Otherwise an update button and a cancel button will be created within the cell.
 * The button captions are specified using {@link setEditText EditText},
 * {@link setUpdateText UpdateText}, and {@link setCancelText CancelText}.
 *
 * This is the active counterpart to the {@link TEditCommandColumn} control. The buttons for
 * interaction are replaced by active buttons.
 *
 * Please refer to the original documentation of the {@link TEditCommandColumn} for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveEditCommandColumn extends TEditCommandColumn {
	protected function createButton($commandName,$text,$causesValidation,$validationGroup) {
		if($this->getButtonType()===TButtonColumnType::LinkButton)
			$button=Prado::createComponent('System.Web.UI.WebControls.TActiveLinkButton');
		else if($this->getButtonType()===TButtonColumnType::PushButton)
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveButton');
			else  // image buttons
			{
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveImageButton');
				$button->setToolTip($text);
				if(strcasecmp($commandName,'Update')===0)
					$url=$this->getUpdateImageUrl();
				else if(strcasecmp($commandName,'Cancel')===0)
						$url=$this->getCancelImageUrl();
					else
						$url=$this->getEditImageUrl();
				$button->setImageUrl($url);
			}
		$button->setText($text);
		$button->setCommandName($commandName);
		$button->setCausesValidation($causesValidation);
		$button->setValidationGroup($validationGroup);
		return $button;
	}
}


/**
 * TActiveButtonColumn class
 *
 * TActiveButtonColumn contains a user-defined command button, such as Add or Remove,
 * that corresponds with each row in the column.
 *
 * This is the active counterpart to the {@link TButtonColumn} control where the
 * button is replaced by the appropriate active button control.
 *
 * Please refer to the original documentation of the {@link TButtonColumn} for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveButtonColumn extends TButtonColumn {
	public function initializeCell($cell,$columnIndex,$itemType) {
		if($itemType===TListItemType::Item || $itemType===TListItemType::AlternatingItem || $itemType===TListItemType::SelectedItem || $itemType===TListItemType::EditItem) {
			$buttonType=$this->getButtonType();
			if($buttonType===TButtonColumnType::LinkButton)
				$button=new TActiveLinkButton;
			else if($buttonType===TButtonColumnType::PushButton)
					$button=new TActiveButton;
				else // image button
				{
					$button=new TActiveImageButton;
					$button->setImageUrl($this->getImageUrl());
					$button->setToolTip($this->getText());
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
}


/**
 * TActiveTemplateColumn class
 *
 * TActiveTemplateColumn customizes the layout of controls in the column with templates.
 * In particular, you can specify {@link setItemTemplate ItemTemplate},
 * {@link setEditItemTemplate EditItemTemplate}, {@link setHeaderTemplate HeaderTemplate}
 * and {@link setFooterTemplate FooterTemplate} to customize specific
 * type of cells in the column.
 *
 * This is the active counterpart to the {@link TTemplateColumn} control. For that purpose,
 * if sorting is allowed, the header links/buttons are replaced by active controls.
 *
 * Please refer to the original documentation of the {@link TTemplateColumn} for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveTemplateColumn extends TTemplateColumn {
	protected function initializeHeaderCell($cell,$columnIndex) {
		$text=$this->getHeaderText();

		if(($classPath=$this->getHeaderRenderer())!=='') {
			$control=Prado::createComponent($classPath);
			if($control instanceof IDataRenderer) {
				if($control instanceof IItemDataRenderer) {
					$item=$cell->getParent();
					$control->setItemIndex($item->getItemIndex());
					$control->setItemType($item->getItemType());
				}
				$control->setData($text);
			}
			$cell->getControls()->add($control);
		}
		else if($this->getAllowSorting()) {
				$sortExpression=$this->getSortExpression();
				if(($url=$this->getHeaderImageUrl())!=='') {
					$button=Prado::createComponent('System.Web.UI.WebControls.TActiveImageButton');
					$button->setImageUrl($url);
					$button->setCommandName(TDataGrid::CMD_SORT);
					$button->setCommandParameter($sortExpression);
					if($text!=='')
						$button->setAlternateText($text);
					$button->setCausesValidation(false);
					$cell->getControls()->add($button);
				}
				else if($text!=='') {
						$button=Prado::createComponent('System.Web.UI.WebControls.TActiveLinkButton');
						$button->setText($text);
						$button->setCommandName(TDataGrid::CMD_SORT);
						$button->setCommandParameter($sortExpression);
						$button->setCausesValidation(false);
						$cell->getControls()->add($button);
					}
					else
						$cell->setText('&nbsp;');
			}
			else {
				if(($url=$this->getHeaderImageUrl())!=='') {
					$image=Prado::createComponent('System.Web.UI.WebControls.TActiveImage');
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
}

/**
 * TActiveHyperLinkColumn class
 *
 * TActiveHyperLinkColumn contains a hyperlink for each item in the column.
 *
 * This is the active counterpart to the {@link THyperLinkColumn} control. For that purpose,
 * if sorting is allowed, the header links/buttons are replaced by active controls.
 *
 * Please refer to the original documentation of the {@link THyperLinkColumn} for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveHyperLinkColumn extends THyperLinkColumn
{

	protected function initializeHeaderCell($cell,$columnIndex)
	{
		$text=$this->getHeaderText();

		if(($classPath=$this->getHeaderRenderer())!=='')
		{
			$control=Prado::createComponent($classPath);
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
			$cell->getControls()->add($control);
		}
		else if($this->getAllowSorting())
		{
			$sortExpression=$this->getSortExpression();
			if(($url=$this->getHeaderImageUrl())!=='')
			{
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveImageButton');
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
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveLinkButton');
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
				$image=Prado::createComponent('System.Web.UI.WebControls.TActiveImage');
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
}

/**
 * TActiveCheckBoxColumn class
 *
 * TActiveCheckBoxColumn represents a checkbox column that is bound to a field in a data source.
 *
 * This is the active counterpart to the {@link TCheckBoxColumn} control. For that purpose,
 * if sorting is allowed, the header links/buttons are replaced by active controls.
 *
 * Please refer to the original documentation of the {@link TCheckBoxColumn} for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveCheckBoxColumn extends TCheckBoxColumn
{
	/**
	 * Initializes the specified cell to its initial values.
	 * This method overrides the parent implementation.
	 * It creates a checkbox inside the cell.
	 * If the column is read-only or if the item is not in edit mode,
	 * the checkbox will be set disabled.
	 * @param TTableCell the cell to be initialized.
	 * @param integer the index to the Columns property that the cell resides in.
	 * @param string the type of cell (Header,Footer,Item,AlternatingItem,EditItem,SelectedItem)
	 */
	public function initializeCell($cell,$columnIndex,$itemType)
	{
		if($itemType===TListItemType::Item || $itemType===TListItemType::AlternatingItem || $itemType===TListItemType::SelectedItem || $itemType===TListItemType::EditItem)
		{
			$checkBox=new TActiveCheckBox;
			if($this->getReadOnly() || $itemType!==TListItemType::EditItem)
				$checkBox->setEnabled(false);
			$cell->setHorizontalAlign('Center');
			$cell->getControls()->add($checkBox);
			$cell->registerObject('CheckBox',$checkBox);
			if($this->getDataField()!=='')
				$checkBox->attachEventHandler('OnDataBinding',array($this,'dataBindColumn'));
		}
		else
			parent::initializeCell($cell,$columnIndex,$itemType);
	}

	protected function initializeHeaderCell($cell,$columnIndex)
	{
		$text=$this->getHeaderText();

		if(($classPath=$this->getHeaderRenderer())!=='')
		{
			$control=Prado::createComponent($classPath);
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
			$cell->getControls()->add($control);
		}
		else if($this->getAllowSorting())
		{
			$sortExpression=$this->getSortExpression();
			if(($url=$this->getHeaderImageUrl())!=='')
			{
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveImageButton');
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
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveLinkButton');
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
				$image=Prado::createComponent('System.Web.UI.WebControls.TActiveImage');
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
}

/**
 * TActiveDropDownListColumn class
 *
 * TActiveDropDownListColumn represents a column that is bound to a field in a data source.
 *
 * This is the active counterpart to the {@link TDropDownListColumn} control. For that purpose,
 * if sorting is allowed, the header links/buttons are replaced by active controls.
 *
 * Please refer to the original documentation of the {@link TDropDownListColumn} for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveDropDownListColumn extends TDropDownListColumn
{
	protected function initializeHeaderCell($cell,$columnIndex)
	{
		$text=$this->getHeaderText();

		if(($classPath=$this->getHeaderRenderer())!=='')
		{
			$control=Prado::createComponent($classPath);
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
			$cell->getControls()->add($control);
		}
		else if($this->getAllowSorting())
		{
			$sortExpression=$this->getSortExpression();
			if(($url=$this->getHeaderImageUrl())!=='')
			{
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveImageButton');
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
				$button=Prado::createComponent('System.Web.UI.WebControls.TActiveLinkButton');
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
				$image=Prado::createComponent('System.Web.UI.WebControls.TActiveImage');
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

}

/**
 * TActiveLiteralColumn class
 *
 * TActiveLiteralColumn represents a static text column that is bound to a field in a data source.
 * The cells in the column will be displayed with static texts using the data indexed by
 * {@link setDataField DataField}. You can customize the display by
 * setting {@link setDataFormatString DataFormatString}.
 *
 * If {@link setDataField DataField} is not specified, the cells will be filled
 * with {@link setText Text}.
 *
 * If {@link setEncode Encode} is true, the static texts will be HTML-encoded.
 *
 * This is the active counterpart to the {@link TLiteralColumn} control. For that purpose,
 * if sorting is allowed, the header links/buttons are replaced by active controls.
 *
 * Please refer to the original documentation of the {@link TLiteralColumn} for usage.
 *
 * @author Fabio Bas <ctrlaltca@gmail.com>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveLiteralColumn extends TLiteralColumn {
	protected function initializeHeaderCell($cell,$columnIndex) {
		$text=$this->getHeaderText();

		if(($classPath=$this->getHeaderRenderer())!=='') {
			$control=Prado::createComponent($classPath);
			if($control instanceof IDataRenderer) {
				if($control instanceof IItemDataRenderer) {
					$item=$cell->getParent();
					$control->setItemIndex($item->getItemIndex());
					$control->setItemType($item->getItemType());
				}
				$control->setData($text);
			}
			$cell->getControls()->add($control);
		}
		else if($this->getAllowSorting()) {
				$sortExpression=$this->getSortExpression();
				if(($url=$this->getHeaderImageUrl())!=='') {
					$button=Prado::createComponent('System.Web.UI.WebControls.TActiveImageButton');
					$button->setImageUrl($url);
					$button->setCommandName(TDataGrid::CMD_SORT);
					$button->setCommandParameter($sortExpression);
					if($text!=='') {
						$button->setAlternateText($text);
						$button->setToolTip($text);
					}
					$button->setCausesValidation(false);
					$cell->getControls()->add($button);
				}
				else if($text!=='') {
						$button=Prado::createComponent('System.Web.UI.WebControls.TActiveLinkButton');
						$button->setText($text);
						$button->setCommandName(TDataGrid::CMD_SORT);
						$button->setCommandParameter($sortExpression);
						$button->setCausesValidation(false);
						$cell->getControls()->add($button);
					}
					else
						$cell->setText('&nbsp;');
			}
			else {
				if(($url=$this->getHeaderImageUrl())!=='') {
					$image=Prado::createComponent('System.Web.UI.WebControls.TActiveImage');
					$image->setImageUrl($url);
					if($text!=='') {
						$image->setAlternateText($text);
						$image->setToolTip($text);
					}
					$cell->getControls()->add($image);
				}
				else if($text!=='')
						$cell->setText($text);
					else
						$cell->setText('&nbsp;');
			}
	}
}
