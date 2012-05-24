<?php
/**
 * TRepeater class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TRepeater.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Using TDataBoundControl and TDataFieldAccessor cass
 */
Prado::using('System.Web.UI.WebControls.TDataBoundControl');
Prado::using('System.Util.TDataFieldAccessor');

/**
 * TRepeater class.
 *
 * TRepeater displays its content repeatedly based on the data fetched from
 * {@link setDataSource DataSource}.
 * The repeated contents in TRepeater are called items, which are controls and
 * can be accessed through {@link getItems Items}. When {@link dataBind()} is invoked,
 * TRepeater creates an item for each row of data and binds the data row to the item.
 * Optionally, a repeater can have a header, a footer and/or separators between items.
 *
 * The layout of the repeated contents are specified by inline templates.
 * Repeater items, header, footer, etc. are being instantiated with the corresponding
 * templates when data is being bound to the repeater.
 *
 * Since v3.1.0, the layout can also be specified by renderers. A renderer is a control class
 * that can be instantiated as repeater items, header, etc. A renderer can thus be viewed
 * as an external template (in fact, it can also be non-templated controls).
 *
 * A renderer can be any control class.
 * - If the class implements {@link IDataRenderer}, the <b>Data</b>
 * property will be set as the data row during databinding. Many PRADO controls
 * implement this interface, such as {@link TLabel}, {@link TTextBox}, etc.
 * - If the class implements {@link IItemDataRenderer}, the <b>ItemIndex</b> property will be set
 * as the zero-based index of the item in the repeater item collection, and
 * the <b>ItemType</b> property as the item's type (such as TListItemType::Item).
 * {@link TRepeaterItemRenderer} may be used as the convenient base class which
 * already implements {@link IDataItemRenderer}.
 *
 * The following properties are used to specify different types of template and renderer
 * for a repeater:
 * - {@link setItemTemplate ItemTemplate}, {@link setItemRenderer ItemRenderer}:
 * for each repeated row of data
 * - {@link setAlternatingItemTemplate AlternatingItemTemplate}, {@link setAlternatingItemRenderer AlternatingItemRenderer}:
 * for each alternating row of data. If not set, {@link setItemTemplate ItemTemplate} or {@link setItemRenderer ItemRenderer}
 * will be used instead.
 * - {@link setHeaderTemplate HeaderTemplate}, {@link setHeaderRenderer HeaderRenderer}:
 * for the repeater header.
 * - {@link setFooterTemplate FooterTemplate}, {@link setFooterRenderer FooterRenderer}:
 * for the repeater footer.
 * - {@link setSeparatorTemplate SeparatorTemplate}, {@link setSeparatorRenderer SeparatorRenderer}:
 * for content to be displayed between items.
 * - {@link setEmptyTemplate EmptyTemplate}, {@link setEmptyRenderer EmptyRenderer}:
 * used when data bound to the repeater is empty.
 *
 * If a content type is defined with both a template and a renderer, the latter takes precedence.
 *
 * When {@link dataBind()} is being called, TRepeater undergoes the following lifecycles for each row of data:
 * - create item based on templates or renderers
 * - set the row of data to the item
 * - raise {@link onItemCreated OnItemCreated}:
 * - add the item as a child control
 * - call dataBind() of the item
 * - raise {@link onItemDataBound OnItemDataBound}:
 *
 * TRepeater raises an {@link onItemCommand OnItemCommand} whenever a button control
 * within some repeater item raises a <b>OnCommand</b> event. Therefore,
 * you can handle all sorts of <b>OnCommand</b> event in a central place by
 * writing an event handler for {@link onItemCommand OnItemCommand}.
 *
 * When a page containing a repeater is post back, the repeater will restore automatically
 * all its contents, including items, header, footer and separators.
 * However, the data row associated with each item will not be recovered and become null.
 * To access the data, use one of the following ways:
 * - Use {@link getDataKeys DataKeys} to obtain the data key associated with
 * the specified repeater item and use the key to fetch the corresponding data
 * from some persistent storage such as DB.
 * - Save the whole dataset in viewstate, which will restore the dataset automatically upon postback.
 * Be aware though, if the size of your dataset is big, your page size will become big. Some
 * complex data may also have serializing problem if saved in viewstate.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeater.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRepeater extends TDataBoundControl implements INamingContainer
{
	/**
	 * Repeater item types
	 * @deprecated deprecated since version 3.0.4. Use TListItemType constants instead.
	 */
	const IT_HEADER='Header';
	const IT_FOOTER='Footer';
	const IT_ITEM='Item';
	const IT_SEPARATOR='Separator';
	const IT_ALTERNATINGITEM='AlternatingItem';

	/**
	 * @var ITemplate template for repeater items
	 */
	private $_itemTemplate=null;
	/**
	 * @var ITemplate template for each alternating item
	 */
	private $_alternatingItemTemplate=null;
	/**
	 * @var ITemplate template for header
	 */
	private $_headerTemplate=null;
	/**
	 * @var ITemplate template for footer
	 */
	private $_footerTemplate=null;
	/**
	 * @var ITemplate template used for repeater when no data is bound
	 */
	private $_emptyTemplate=null;
	/**
	 * @var ITemplate template for separator
	 */
	private $_separatorTemplate=null;
	/**
	 * @var TRepeaterItemCollection list of repeater items
	 */
	private $_items=null;
	/**
	 * @var TControl header item
	 */
	private $_header=null;
	/**
	 * @var TControl footer item
	 */
	private $_footer=null;


	/**
	 * @return string the class name for repeater items. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getItemRenderer()
	{
		return $this->getViewState('ItemRenderer','');
	}

	/**
	 * Sets the item renderer class.
	 *
	 * If not empty, the class will be used to instantiate as repeater items.
	 * This property takes precedence over {@link getItemTemplate ItemTemplate}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @see setItemTemplate
	 * @since 3.1.0
	 */
	public function setItemRenderer($value)
	{
		$this->setViewState('ItemRenderer',$value,'');
	}

	/**
	 * @return string the class name for alternative repeater items. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getAlternatingItemRenderer()
	{
		return $this->getViewState('AlternatingItemRenderer','');
	}

	/**
	 * Sets the alternative item renderer class.
	 *
	 * If not empty, the class will be used to instantiate as alternative repeater items.
	 * This property takes precedence over {@link getAlternatingItemTemplate AlternatingItemTemplate}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @see setAlternatingItemTemplate
	 * @since 3.1.0
	 */
	public function setAlternatingItemRenderer($value)
	{
		$this->setViewState('AlternatingItemRenderer',$value,'');
	}

	/**
	 * @return string the class name for repeater item separators. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getSeparatorRenderer()
	{
		return $this->getViewState('SeparatorRenderer','');
	}

	/**
	 * Sets the repeater item separator renderer class.
	 *
	 * If not empty, the class will be used to instantiate as repeater item separators.
	 * This property takes precedence over {@link getSeparatorTemplate SeparatorTemplate}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @see setSeparatorTemplate
	 * @since 3.1.0
	 */
	public function setSeparatorRenderer($value)
	{
		$this->setViewState('SeparatorRenderer',$value,'');
	}

	/**
	 * @return string the class name for repeater header item. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getHeaderRenderer()
	{
		return $this->getViewState('HeaderRenderer','');
	}

	/**
	 * Sets the repeater header renderer class.
	 *
	 * If not empty, the class will be used to instantiate as repeater header item.
	 * This property takes precedence over {@link getHeaderTemplate HeaderTemplate}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @see setHeaderTemplate
	 * @since 3.1.0
	 */
	public function setHeaderRenderer($value)
	{
		$this->setViewState('HeaderRenderer',$value,'');
	}

	/**
	 * @return string the class name for repeater footer item. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getFooterRenderer()
	{
		return $this->getViewState('FooterRenderer','');
	}

	/**
	 * Sets the repeater footer renderer class.
	 *
	 * If not empty, the class will be used to instantiate as repeater footer item.
	 * This property takes precedence over {@link getFooterTemplate FooterTemplate}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @see setFooterTemplate
	 * @since 3.1.0
	 */
	public function setFooterRenderer($value)
	{
		$this->setViewState('FooterRenderer',$value,'');
	}

	/**
	 * @return string the class name for empty repeater item. Defaults to empty, meaning not set.
	 * @since 3.1.0
	 */
	public function getEmptyRenderer()
	{
		return $this->getViewState('EmptyRenderer','');
	}

	/**
	 * Sets the repeater empty renderer class.
	 *
	 * The empty renderer is created as the child of the repeater
	 * if data bound to the repeater is empty.
	 * This property takes precedence over {@link getEmptyTemplate EmptyTemplate}.
	 *
	 * @param string the renderer class name in namespace format.
	 * @see setEmptyTemplate
	 * @since 3.1.0
	 */
	public function setEmptyRenderer($value)
	{
		$this->setViewState('EmptyRenderer',$value,'');
	}

	/**
	 * @return ITemplate the template for repeater items
	 */
	public function getItemTemplate()
	{
		return $this->_itemTemplate;
	}

	/**
	 * @param ITemplate the template for repeater items
	 * @throws TInvalidDataTypeException if the input is not an {@link ITemplate} or not null.
	 */
	public function setItemTemplate($value)
	{
		if($value instanceof ITemplate || $value===null)
			$this->_itemTemplate=$value;
		else
			throw new TInvalidDataTypeException('repeater_template_required','ItemTemplate');
	}

	/**
	 * @return ITemplate the alternative template string for the item
	 */
	public function getAlternatingItemTemplate()
	{
		return $this->_alternatingItemTemplate;
	}

	/**
	 * @param ITemplate the alternative item template
	 * @throws TInvalidDataTypeException if the input is not an {@link ITemplate} or not null.
	 */
	public function setAlternatingItemTemplate($value)
	{
		if($value instanceof ITemplate || $value===null)
			$this->_alternatingItemTemplate=$value;
		else
			throw new TInvalidDataTypeException('repeater_template_required','AlternatingItemTemplate');
	}

	/**
	 * @return ITemplate the header template
	 */
	public function getHeaderTemplate()
	{
		return $this->_headerTemplate;
	}

	/**
	 * @param ITemplate the header template
	 * @throws TInvalidDataTypeException if the input is not an {@link ITemplate} or not null.
	 */
	public function setHeaderTemplate($value)
	{
		if($value instanceof ITemplate || $value===null)
			$this->_headerTemplate=$value;
		else
			throw new TInvalidDataTypeException('repeater_template_required','HeaderTemplate');
	}

	/**
	 * @return ITemplate the footer template
	 */
	public function getFooterTemplate()
	{
		return $this->_footerTemplate;
	}

	/**
	 * @param ITemplate the footer template
	 * @throws TInvalidDataTypeException if the input is not an {@link ITemplate} or not null.
	 */
	public function setFooterTemplate($value)
	{
		if($value instanceof ITemplate || $value===null)
			$this->_footerTemplate=$value;
		else
			throw new TInvalidDataTypeException('repeater_template_required','FooterTemplate');
	}

	/**
	 * @return ITemplate the template applied when no data is bound to the repeater
	 */
	public function getEmptyTemplate()
	{
		return $this->_emptyTemplate;
	}

	/**
	 * @param ITemplate the template applied when no data is bound to the repeater
	 * @throws TInvalidDataTypeException if the input is not an {@link ITemplate} or not null.
	 */
	public function setEmptyTemplate($value)
	{
		if($value instanceof ITemplate || $value===null)
			$this->_emptyTemplate=$value;
		else
			throw new TInvalidDataTypeException('repeater_template_required','EmptyTemplate');
	}

	/**
	 * @return ITemplate the separator template
	 */
	public function getSeparatorTemplate()
	{
		return $this->_separatorTemplate;
	}

	/**
	 * @param ITemplate the separator template
	 * @throws TInvalidDataTypeException if the input is not an {@link ITemplate} or not null.
	 */
	public function setSeparatorTemplate($value)
	{
		if($value instanceof ITemplate || $value===null)
			$this->_separatorTemplate=$value;
		else
			throw new TInvalidDataTypeException('repeater_template_required','SeparatorTemplate');
	}

	/**
	 * @return TControl the header item
	 */
	public function getHeader()
	{
		return $this->_header;
	}

	/**
	 * @return TControl the footer item
	 */
	public function getFooter()
	{
		return $this->_footer;
	}

	/**
	 * @return TRepeaterItemCollection list of repeater item controls
	 */
	public function getItems()
	{
		if(!$this->_items)
			$this->_items=new TRepeaterItemCollection;
		return $this->_items;
	}

	/**
	 * @return string the field of the data source that provides the keys of the list items.
	 */
	public function getDataKeyField()
	{
		return $this->getViewState('DataKeyField','');
	}

	/**
	 * @param string the field of the data source that provides the keys of the list items.
	 */
	public function setDataKeyField($value)
	{
		$this->setViewState('DataKeyField',$value,'');
	}

	/**
	 * @return TList the keys used in the data listing control.
	 */
	public function getDataKeys()
	{
		if(($dataKeys=$this->getViewState('DataKeys',null))===null)
		{
			$dataKeys=new TList;
			$this->setViewState('DataKeys',$dataKeys,null);
		}
		return $dataKeys;
	}

	/**
	 * Creates a repeater item.
	 * This method invokes {@link createItem} to create a new repeater item.
	 * @param integer zero-based item index.
	 * @param TListItemType item type
	 * @return TControl the created item, null if item is not created
	 */
	private function createItemInternal($itemIndex,$itemType)
	{
		if(($item=$this->createItem($itemIndex,$itemType))!==null)
		{
			$param=new TRepeaterItemEventParameter($item);
			$this->onItemCreated($param);
			$this->getControls()->add($item);
			return $item;
		}
		else
			return null;
	}

	/**
	 * Creates a repeater item and performs databinding.
	 * This method invokes {@link createItem} to create a new repeater item.
	 * @param integer zero-based item index.
	 * @param TListItemType item type
	 * @param mixed data to be associated with the item
	 * @return TControl the created item, null if item is not created
	 */
	private function createItemWithDataInternal($itemIndex,$itemType,$dataItem)
	{
		if(($item=$this->createItem($itemIndex,$itemType))!==null)
		{
			$param=new TRepeaterItemEventParameter($item);
			if($item instanceof IDataRenderer)
				$item->setData($dataItem);
			$this->onItemCreated($param);
			$this->getControls()->add($item);
			$item->dataBind();
			$this->onItemDataBound($param);
			return $item;
		}
		else
			return null;
	}

	/**
	 * Creates a repeater item instance based on the item type and index.
	 * @param integer zero-based item index
	 * @param TListItemType item type
	 * @return TControl created repeater item
	 */
	protected function createItem($itemIndex,$itemType)
	{
		$template=null;
		$classPath=null;
		switch($itemType)
		{
			case TListItemType::Item :
				$classPath=$this->getItemRenderer();
				$template=$this->_itemTemplate;
				break;
			case TListItemType::AlternatingItem :
				if(($classPath=$this->getAlternatingItemRenderer())==='' && ($template=$this->_alternatingItemTemplate)===null)
				{
					$classPath=$this->getItemRenderer();
					$template=$this->_itemTemplate;
				}
				break;
			case TListItemType::Header :
				$classPath=$this->getHeaderRenderer();
				$template=$this->_headerTemplate;
				break;
			case TListItemType::Footer :
				$classPath=$this->getFooterRenderer();
				$template=$this->_footerTemplate;
				break;
			case TListItemType::Separator :
				$classPath=$this->getSeparatorRenderer();
				$template=$this->_separatorTemplate;
				break;
			default:
				throw new TInvalidDataValueException('repeater_itemtype_unknown',$itemType);
		}
		if($classPath!=='')
		{
			$item=Prado::createComponent($classPath);
			if($item instanceof IItemDataRenderer)
			{
				$item->setItemIndex($itemIndex);
				$item->setItemType($itemType);
			}
		}
		else if($template!==null)
		{
			$item=new TRepeaterItem;
			$item->setItemIndex($itemIndex);
			$item->setItemType($itemType);
			$template->instantiateIn($item);
		}
		else
			$item=null;

		return $item;
	}

	/**
	 * Creates empty repeater content.
	 */
	protected function createEmptyContent()
	{
		if(($classPath=$this->getEmptyRenderer())!=='')
			$this->getControls()->add(Prado::createComponent($classPath));
		else if($this->_emptyTemplate!==null)
			$this->_emptyTemplate->instantiateIn($this);
	}

	/**
	 * Renders the repeater.
	 * This method overrides the parent implementation by rendering the body
	 * content as the whole presentation of the repeater. Outer tag is not rendered.
	 * @param THtmlWriter writer
	 */
	public function render($writer)
	{
		if($this->_items && $this->_items->getCount() || $this->_emptyTemplate!==null || $this->getEmptyRenderer()!=='')
			$this->renderContents($writer);
	}

	/**
	 * Saves item count in viewstate.
	 * This method is invoked right before control state is to be saved.
	 */
	public function saveState()
	{
		parent::saveState();
		if($this->_items)
			$this->setViewState('ItemCount',$this->_items->getCount(),0);
		else
			$this->clearViewState('ItemCount');
	}

	/**
	 * Loads item count information from viewstate.
	 * This method is invoked right after control state is loaded.
	 */
	public function loadState()
	{
		parent::loadState();
		if(!$this->getIsDataBound())
			$this->restoreItemsFromViewState();
		$this->clearViewState('ItemCount');
	}

	/**
	 * Clears up all items in the repeater.
	 */
	public function reset()
	{
		$this->getControls()->clear();
		$this->getItems()->clear();
		$this->_header=null;
		$this->_footer=null;
	}

	/**
	 * Creates repeater items based on viewstate information.
	 */
	protected function restoreItemsFromViewState()
	{
		$this->reset();
		if(($itemCount=$this->getViewState('ItemCount',0))>0)
		{
			$items=$this->getItems();
			$hasSeparator=$this->_separatorTemplate!==null || $this->getSeparatorRenderer()!=='';
			$this->_header=$this->createItemInternal(-1,TListItemType::Header);
			for($i=0;$i<$itemCount;++$i)
			{
				if($hasSeparator && $i>0)
					$this->createItemInternal($i-1,TListItemType::Separator);
				$itemType=$i%2==0?TListItemType::Item : TListItemType::AlternatingItem;
				$items->add($this->createItemInternal($i,$itemType,false,null));
			}
			$this->_footer=$this->createItemInternal(-1,TListItemType::Footer);
		}
		else
			$this->createEmptyContent();
		$this->clearChildState();
	}

	/**
	 * Performs databinding to populate repeater items from data source.
	 * This method is invoked by dataBind().
	 * You may override this function to provide your own way of data population.
	 * @param Traversable the data
	 */
	protected function performDataBinding($data)
	{
		$this->reset();

		$keys=$this->getDataKeys();
		$keys->clear();
		$keyField=$this->getDataKeyField();

		$items=$this->getItems();
		$itemIndex=0;
		$hasSeparator=$this->_separatorTemplate!==null || $this->getSeparatorRenderer()!=='';
		foreach($data as $key=>$dataItem)
		{
			if($keyField!=='')
				$keys->add($this->getDataFieldValue($dataItem,$keyField));
			else
				$keys->add($key);
			if($itemIndex===0)
				$this->_header=$this->createItemWithDataInternal(-1,TListItemType::Header,null);
			if($hasSeparator && $itemIndex>0)
				$this->createItemWithDataInternal($itemIndex-1,TListItemType::Separator,null);
			$itemType=$itemIndex%2==0?TListItemType::Item : TListItemType::AlternatingItem;
			$items->add($this->createItemWithDataInternal($itemIndex,$itemType,$dataItem));
			$itemIndex++;
		}
		if($itemIndex>0)
			$this->_footer=$this->createItemWithDataInternal(-1,TListItemType::Footer,null);
		else
		{
			$this->createEmptyContent();
			$this->dataBindChildren();
		}
		$this->setViewState('ItemCount',$itemIndex,0);
	}

	/**
	 * This method overrides parent's implementation to handle
	 * {@link onItemCommand OnItemCommand} event which is bubbled from
	 * repeater items and their child controls.
	 * This method should only be used by control developers.
	 * @param TControl the sender of the event
	 * @param TEventParameter event parameter
	 * @return boolean whether the event bubbling should stop here.
	 */
	public function bubbleEvent($sender,$param)
	{
		if($param instanceof TRepeaterCommandEventParameter)
		{
			$this->onItemCommand($param);
			return true;
		}
		else
			return false;
	}

	/**
	 * Raises <b>OnItemCreated</b> event.
	 * This method is invoked after a repeater item is created and instantiated with
	 * template, but before added to the page hierarchy.
	 * The repeater item control responsible for the event
	 * can be determined from the event parameter.
	 * If you override this method, be sure to call parent's implementation
	 * so that event handlers have chance to respond to the event.
	 * @param TRepeaterItemEventParameter event parameter
	 */
	public function onItemCreated($param)
	{
		$this->raiseEvent('OnItemCreated',$this,$param);
	}

	/**
	 * Raises <b>OnItemDataBound</b> event.
	 * This method is invoked right after an item is data bound.
	 * The repeater item control responsible for the event
	 * can be determined from the event parameter.
	 * If you override this method, be sure to call parent's implementation
	 * so that event handlers have chance to respond to the event.
	 * @param TRepeaterItemEventParameter event parameter
	 */
	public function onItemDataBound($param)
	{
		$this->raiseEvent('OnItemDataBound',$this,$param);
	}

	/**
	 * Raises <b>OnItemCommand</b> event.
	 * This method is invoked after a button control in
	 * a template raises <b>OnCommand</b> event.
	 * The repeater control responsible for the event
	 * can be determined from the event parameter.
	 * The event parameter also contains the information about
	 * the initial sender of the <b>OnCommand</b> event, command name
	 * and command parameter.
	 * You may override this method to provide customized event handling.
	 * Be sure to call parent's implementation so that
	 * event handlers have chance to respond to the event.
	 * @param TRepeaterCommandEventParameter event parameter
	 */
	public function onItemCommand($param)
	{
		$this->raiseEvent('OnItemCommand',$this,$param);
	}

	/**
	 * Returns the value of the data at the specified field.
	 * If data is an array, TMap or TList, the value will be returned at the index
	 * of the specified field. If the data is a component with a property named
	 * as the field name, the property value will be returned.
	 * Otherwise, an exception will be raised.
	 * @param mixed data item
	 * @param mixed field name
	 * @return mixed data value at the specified field
	 * @throws TInvalidDataValueException if the data is invalid
	 */
	protected function getDataFieldValue($data,$field)
	{
		return TDataFieldAccessor::getDataFieldValue($data,$field);
	}
}

/**
 * TRepeaterItemEventParameter class
 *
 * TRepeaterItemEventParameter encapsulates the parameter data for
 * {@link TRepeater::onItemCreated ItemCreated} event of {@link TRepeater} controls.
 * The {@link getItem Item} property indicates the repeater item related with the event.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeater.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRepeaterItemEventParameter extends TEventParameter
{
	/**
	 * The repeater item control responsible for the event.
	 * @var TControl
	 */
	private $_item=null;

	/**
	 * Constructor.
	 * @param TControl repeater item related with the corresponding event
	 */
	public function __construct($item)
	{
		$this->_item=$item;
	}

	/**
	 * @return TControl repeater item related with the corresponding event
	 */
	public function getItem()
	{
		return $this->_item;
	}
}

/**
 * TRepeaterCommandEventParameter class
 *
 * TRepeaterCommandEventParameter encapsulates the parameter data for
 * {@link TRepeater::onItemCommand ItemCommand} event of {@link TRepeater} controls.
 *
 * The {@link getItem Item} property indicates the repeater item related with the event.
 * The {@link getCommandSource CommandSource} refers to the control that originally
 * raises the Command event.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeater.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRepeaterCommandEventParameter extends TCommandEventParameter
{
	/**
	 * @var TControl the repeater item control responsible for the event.
	 */
	private $_item=null;
	/**
	 * @var TControl the control originally raises the <b>OnCommand</b> event.
	 */
	private $_source=null;

	/**
	 * Constructor.
	 * @param TControl repeater item responsible for the event
	 * @param TControl original event sender
	 * @param TCommandEventParameter original event parameter
	 */
	public function __construct($item,$source,TCommandEventParameter $param)
	{
		$this->_item=$item;
		$this->_source=$source;
		parent::__construct($param->getCommandName(),$param->getCommandParameter());
	}

	/**
	 * @return TControl the repeater item control responsible for the event.
	 */
	public function getItem()
	{
		return $this->_item;
	}

	/**
	 * @return TControl the control originally raises the <b>OnCommand</b> event.
	 */
	public function getCommandSource()
	{
		return $this->_source;
	}
}

/**
 * TRepeaterItem class
 *
 * A TRepeaterItem control represents an item in the {@link TRepeater} control,
 * such as heading section, footer section, or a data item.
 * The index and data value of the item can be accessed via {@link getItemIndex ItemIndex}>
 * and {@link getDataItem DataItem} properties, respectively. The type of the item
 * is given by {@link getItemType ItemType} property.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeater.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRepeaterItem extends TControl implements INamingContainer, IItemDataRenderer
{
	/**
	 * index of the data item in the Items collection of repeater
	 */
	private $_itemIndex;
	/**
	 * type of the TRepeaterItem
	 * @var TListItemType
	 */
	private $_itemType;
	/**
	 * data associated with this item
	 * @var mixed
	 */
	private $_data;

	/**
	 * @return TListItemType item type
	 */
	public function getItemType()
	{
		return $this->_itemType;
	}

	/**
	 * @param TListItemType item type.
	 */
	public function setItemType($value)
	{
		$this->_itemType=TPropertyValue::ensureEnum($value,'TListItemType');
	}

	/**
	 * Returns a value indicating the zero-based index of the item in the corresponding data control's item collection.
	 * If the item is not in the collection (e.g. it is a header item), it returns -1.
	 * @return integer zero-based index of the item.
	 */
	public function getItemIndex()
	{
		return $this->_itemIndex;
	}

	/**
	 * Sets the zero-based index for the item.
	 * If the item is not in the item collection (e.g. it is a header item), -1 should be used.
	 * @param integer zero-based index of the item.
	 */
	public function setItemIndex($value)
	{
		$this->_itemIndex=TPropertyValue::ensureInteger($value);
	}

	/**
	 * @return mixed data associated with the item
	 * @since 3.1.0
	 */
	public function getData()
	{
		return $this->_data;
	}

	/**
	 * @param mixed data to be associated with the item
	 * @since 3.1.0
	 */
	public function setData($value)
	{
		$this->_data=$value;
	}

	/**
	 * This property is deprecated since v3.1.0.
	 * @return mixed data associated with the item
	 * @deprecated deprecated since v3.1.0. Use {@link getData} instead.
	 */
	public function getDataItem()
	{
		return $this->getData();
	}

	/**
	 * This property is deprecated since v3.1.0.
	 * @param mixed data to be associated with the item
	 * @deprecated deprecated since version 3.1.0. Use {@link setData} instead.
	 */
	public function setDataItem($value)
	{
		return $this->setData($value);
	}

	/**
	 * This method overrides parent's implementation by wrapping event parameter
	 * for <b>OnCommand</b> event with item information.
	 * @param TControl the sender of the event
	 * @param TEventParameter event parameter
	 * @return boolean whether the event bubbling should stop here.
	 */
	public function bubbleEvent($sender,$param)
	{
		if($param instanceof TCommandEventParameter)
		{
			$this->raiseBubbleEvent($this,new TRepeaterCommandEventParameter($this,$sender,$param));
			return true;
		}
		else
			return false;
	}
}


/**
 * TRepeaterItemCollection class.
 *
 * TRepeaterItemCollection represents a collection of repeater items.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeater.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRepeaterItemCollection extends TList
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by inserting only objects that are descendant of {@link TControl}.
	 * @param integer the speicified position.
	 * @param TControl new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a control.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TControl)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('repeateritemcollection_item_invalid');
	}
}

