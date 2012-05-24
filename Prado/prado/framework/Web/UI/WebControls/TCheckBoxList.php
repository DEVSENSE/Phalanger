<?php
/**
 * TCheckBoxList class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TCheckBoxList.php 2702 2009-07-27 10:47:39Z carlgmathisen $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TListControl class
 */
Prado::using('System.Web.UI.WebControls.TListControl');
/**
 * Includes TRepeatInfo class
 */
Prado::using('System.Web.UI.WebControls.TRepeatInfo');
/**
 * Includes TCheckBox class
 */
Prado::using('System.Web.UI.WebControls.TCheckBox');

/**
 * TCheckBoxList class
 *
 * TCheckBoxList displays a list of checkboxes on a Web page.
 *
 * The layout of the checkbox list is specified via {@link setRepeatLayout RepeatLayout},
 * which can be either 'Table' (default) or 'Flow'.
 * A table layout uses HTML table cells to organize the checkboxes while
 * a flow layout uses line breaks to organize the checkboxes.
 * When the layout is using 'Table', {@link setCellPadding CellPadding} and
 * {@link setCellSpacing CellSpacing} can be used to adjust the cellpadding and
 * cellpadding of the table.
 *
 * The number of columns used to display the checkboxes is specified via
 * {@link setRepeatColumns RepeatColumns} property, while the {@link setRepeatDirection RepeatDirection}
 * governs the order of the items being rendered.
 *
 * The alignment of the text besides each checkbox can be specified via {@link setTextAlign TextAlign}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TCheckBoxList.php 2702 2009-07-27 10:47:39Z carlgmathisen $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TCheckBoxList extends TListControl implements IRepeatInfoUser, INamingContainer, IPostBackDataHandler,  IValidatable
{
	private $_repeatedControl;
	private $_isEnabled;
	private $_changedEventRaised=false;
	private $_dataChanged=false;
	private $_isValid=true;

	/**
	 * Constructor.
	 * Remember to call parent implementation if you override this method
	 */
	public function __construct()
	{
		parent::__construct();
		$this->_repeatedControl=$this->createRepeatedControl();
		$this->_repeatedControl->setEnableViewState(false);
		$this->_repeatedControl->setID('c0');
		$this->getControls()->add($this->_repeatedControl);
	}

	/**
	 * Creates a control used for repetition (used as a template).
	 * @return TControl the control to be repeated
	 */
	protected function createRepeatedControl()
	{
		return new TCheckBox;
	}

	/**
	 * Finds a control by ID.
	 * This method overrides the parent implementation so that it always returns
	 * the checkbox list itself (because the checkbox list does not have child controls.)
	 * @param string control ID
	 * @return TControl control being found
	 */
	public function findControl($id)
	{
		return $this;
	}

	/**
	 * @return boolean whether this control supports multiple selection. Always true for checkbox list.
	 */
	protected function getIsMultiSelect()
	{
		return true;
	}

	/**
	 * Creates a style object for the control.
	 * This method creates a {@link TTableStyle} to be used by checkbox list.
	 * @return TStyle control style to be used
	 */
	protected function createStyle()
	{
		return new TTableStyle;
	}

	/**
	 * @return TTextAlign the alignment of the text caption, defaults to TTextAlign::Right.
	 */
	public function getTextAlign()
	{
		return $this->getViewState('TextAlign',TTextAlign::Right);
	}

	/**
	 * @param TTextAlign the text alignment of the checkboxes
	 */
	public function setTextAlign($value)
	{
		$this->setViewState('TextAlign',TPropertyValue::ensureEnum($value,'TTextAlign'),TTextAlign::Right);
	}


	/**
	 * @return TRepeatInfo repeat information (primarily used by control developers)
	 */
	protected function getRepeatInfo()
	{
		if(($repeatInfo=$this->getViewState('RepeatInfo',null))===null)
		{
			$repeatInfo=new TRepeatInfo;
			$this->setViewState('RepeatInfo',$repeatInfo,null);
		}
		return $repeatInfo;
	}

	/**
	 * @return integer the number of columns that the list should be displayed with. Defaults to 0 meaning not set.
	 */
	public function getRepeatColumns()
	{
		return $this->getRepeatInfo()->getRepeatColumns();
	}

	/**
	 * @param integer the number of columns that the list should be displayed with.
	 */
	public function setRepeatColumns($value)
	{
		$this->getRepeatInfo()->setRepeatColumns($value);
	}

	/**
	 * @return string the direction of traversing the list, defaults to 'Vertical'
	 */
	public function getRepeatDirection()
	{
		return $this->getRepeatInfo()->getRepeatDirection();
	}

	/**
	 * @param string the direction (Vertical, Horizontal) of traversing the list
	 */
	public function setRepeatDirection($value)
	{
		$this->getRepeatInfo()->setRepeatDirection($value);
	}

	/**
	 * @return string how the list should be displayed, using table or using line breaks. Defaults to 'Table'.
	 */
	public function getRepeatLayout()
	{
		return $this->getRepeatInfo()->getRepeatLayout();
	}

	/**
	 * @param string how the list should be displayed, using table or using line breaks (Table, Flow)
	 */
	public function setRepeatLayout($value)
	{
		$this->getRepeatInfo()->setRepeatLayout($value);
	}

	/**
	 * @return integer the cellspacing for the table keeping the checkbox list. Defaults to -1, meaning not set.
	 */
	public function getCellSpacing()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getCellSpacing();
		else
			return -1;
	}

	/**
	 * Sets the cellspacing for the table keeping the checkbox list.
	 * @param integer the cellspacing for the table keeping the checkbox list.
	 */
	public function setCellSpacing($value)
	{
		$this->getStyle()->setCellSpacing($value);
	}

	/**
	 * @return integer the cellpadding for the table keeping the checkbox list. Defaults to -1, meaning not set.
	 */
	public function getCellPadding()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getCellPadding();
		else
			return -1;
	}

	/**
	 * Sets the cellpadding for the table keeping the checkbox list.
	 * @param integer the cellpadding for the table keeping the checkbox list.
	 */
	public function setCellPadding($value)
	{
		$this->getStyle()->setCellPadding($value);
	}

	/**
	 * Returns a value indicating whether this control contains header item.
	 * This method is required by {@link IRepeatInfoUser} interface.
	 * @return boolean always false.
	 */
	public function getHasHeader()
	{
		return false;
	}

	/**
	 * Returns a value indicating whether this control contains footer item.
	 * This method is required by {@link IRepeatInfoUser} interface.
	 * @return boolean always false.
	 */
	public function getHasFooter()
	{
		return false;
	}

	/**
	 * Returns a value indicating whether this control contains separator items.
	 * This method is required by {@link IRepeatInfoUser} interface.
	 * @return boolean always false.
	 */
	public function getHasSeparators()
	{
		return false;
	}
	
	/**
	 * @param boolean whether the control is to be enabled.
	 */
	public function setEnabled($value)
	{
		parent::setEnabled($value);
		$value = !TPropertyValue::ensureBoolean($value);
		// if this is an active control, 
		// and it's a callback, 
		// and we can update clientside,
		// then update the 'disabled' attribute of the items.
		if(($this instanceof IActiveControl) &&
				$this->getPage()->getIsCallBack() &&
				$this->getActiveControl()->canUpdateClientSide())
		{
			$items = $this->getItems();
			$cs = $this->getPage()->getCallbackClient();
			$baseClientID = $this->getClientID().'_c';
			foreach($items as $index=>$item)
			{
				$cs->setAttribute($baseClientID.$index, 'disabled', $value);
			}
		}
	}

	/**
	 * Returns a style used for rendering items.
	 * This method is required by {@link IRepeatInfoUser} interface.
	 * @param string item type (Header,Footer,Item,AlternatingItem,SelectedItem,EditItem,Separator,Pager)
	 * @param integer index of the item being rendered
	 * @return null
	 */
	public function generateItemStyle($itemType,$index)
	{
		return null;
	}

	/**
	 * Renders an item in the list.
	 * This method is required by {@link IRepeatInfoUser} interface.
	 * @param THtmlWriter writer for rendering purpose
	 * @param TRepeatInfo repeat information
	 * @param string item type (Header,Footer,Item,AlternatingItem,SelectedItem,EditItem,Separator,Pager)
	 * @param integer zero-based index of the item in the item list
	 */
	public function renderItem($writer,$repeatInfo,$itemType,$index)
	{
		$repeatedControl=$this->_repeatedControl;
		$item=$this->getItems()->itemAt($index);
		if($item->getHasAttributes())
			$repeatedControl->getAttributes()->copyFrom($item->getAttributes());
		else if($repeatedControl->getHasAttributes())
			$repeatedControl->getAttributes()->clear();
		$repeatedControl->setID("c$index");
		$repeatedControl->setText($item->getText());
		$repeatedControl->setChecked($item->getSelected());
		$repeatedControl->setAttribute('value',$item->getValue());
		$repeatedControl->setEnabled($this->_isEnabled && $item->getEnabled());
		$repeatedControl->setEnableClientScript(false);
		$repeatedControl->renderControl($writer);
	}

	/**
	 * Loads user input data.
	 * This method is primarly used by framework developers.
	 * @param string the key that can be used to retrieve data from the input data collection
	 * @param array the input data collection
	 * @return boolean whether the data of the control has been changed
	 */
	public function loadPostData($key,$values)
	{
		if($this->getEnabled(true))
		{
			$index=(int)substr($key,strlen($this->getUniqueID())+2);
			$this->ensureDataBound();
			if($index>=0 && $index<$this->getItemCount())
			{
				$item=$this->getItems()->itemAt($index);
				if($item->getEnabled())
				{
					$checked=isset($values[$key]);
					if($item->getSelected()!==$checked)
					{
						$item->setSelected($checked);
						if(!$this->_changedEventRaised)
						{
							$this->_changedEventRaised=true;
							return $this->_dataChanged=true;
						}
					}
				}
			}
		}
		return false;
	}

	/**
	 * Raises postdata changed event.
	 * This method is required by {@link IPostBackDataHandler} interface.
	 * It is invoked by the framework when {@link getSelectedIndices SelectedIndices} property
	 * is changed on postback.
	 * This method is primarly used by framework developers.
	 */
	public function raisePostDataChangedEvent()
	{
		if($this->getAutoPostBack() && $this->getCausesValidation())
			$this->getPage()->validate($this->getValidationGroup());
		$this->onSelectedIndexChanged(null);
	}

	/**
	 * Registers for post data on postback.
	 * This method overrides the parent implementation.
	 * @param mixed event parameter
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		$this->_repeatedControl->setAutoPostBack($this->getAutoPostBack());
		$this->_repeatedControl->setCausesValidation($this->getCausesValidation());
		$this->_repeatedControl->setValidationGroup($this->getValidationGroup());
		$page=$this->getPage();
		$n=$this->getItemCount();
		for($i=0;$i<$n;++$i)
		{
			$this->_repeatedControl->setID("c$i");
			$page->registerRequiresPostData($this->_repeatedControl);
		}
	}

	/**
	 * Wether the list should be rendered inside a span or not
	 * 
	 *@return boolean true if we need a span
	 */
	protected function getSpanNeeded ()
	{
		return $this->getRepeatLayout()===TRepeatLayout::Raw;
	}
	
	/**
	 * Renders the checkbox list control.
	 * This method overrides the parent implementation.
	 * @param THtmlWriter writer for rendering purpose.
	 */
	public function render($writer)
	{
		if($this->getItemCount()>0)
		{
			if ($needSpan=$this->getSpanNeeded())
			{
				$writer->addAttribute('id', $this->getClientId());
				$writer->renderBeginTag('span');
			}
			$this->_isEnabled=$this->getEnabled(true);
			$repeatInfo=$this->getRepeatInfo();
			$accessKey=$this->getAccessKey();
			$tabIndex=$this->getTabIndex();
			$this->_repeatedControl->setTextAlign($this->getTextAlign());
			$this->_repeatedControl->setAccessKey($accessKey);
			$this->_repeatedControl->setTabIndex($tabIndex);
			$this->setAccessKey('');
			$this->setTabIndex(0);
			$repeatInfo->renderRepeater($writer,$this);
			$this->setAccessKey($accessKey);
			$this->setTabIndex($tabIndex);
			if ($needSpan)
				$writer->renderEndTag();
		}
		//checkbox skipped the client control script in addAttributesToRender
		if($this->getEnabled(true)
			&& $this->getEnableClientScript()
			&& $this->getAutoPostBack()
			&& $this->getPage()->getClientSupportsJavaScript())
		{
			$this->renderClientControlScript($writer);
		}
	}

	/**
	 * Returns a value indicating whether postback has caused the control data change.
	 * This method is required by the IPostBackDataHandler interface.
	 * @return boolean whether postback has caused the control data change. False if the page is not in postback mode.
	 */
	public function getDataChanged()
	{
		return $this->_dataChanged;
	}

	/**
	 * Returns the value to be validated.
	 * This methid is required by IValidatable interface.
	 * @return mixed the value of the property to be validated.
	 */
	public function getValidationPropertyValue()
	{
		return $this->getSelectedValue();
	}

	/**
	 * Returns true if this control validated successfully. 
	 * Defaults to true.
	 * @return bool wether this control validated successfully.
	 */
	public function getIsValid()
	{
	    return $this->_isValid;
	}
	/**
	 * @param bool wether this control is valid.
	 */
	public function setIsValid($value)
	{
	    $this->_isValid=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TCheckBoxList';
	}

	/**
	 * Gets the post back options for this checkbox.
	 * @return array
	 */
	protected function getPostBackOptions()
	{
		$options['ListID'] = $this->getClientID();
		$options['ValidationGroup'] = $this->getValidationGroup();
		$options['CausesValidation'] = $this->getCausesValidation();
		$options['ListName'] = $this->getUniqueID();
		$options['ItemCount'] = $this->getItemCount();
		return $options;
	}
	
}

