<?php
/**
 * TActiveListControlAdapter class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveListControlAdapter.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active control adapter.
 */
Prado::using('System.Web.UI.ActiveControls.TActiveControlAdapter');
Prado::using('System.Web.UI.WebControls.TListControl');

/**
 * TActiveListControlAdapter class.
 *
 * Adapte the list controls to allows the selections on the client-side to be altered
 * during callback response.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveListControlAdapter.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveListControlAdapter extends TActiveControlAdapter implements IListControlAdapter
{
	/**
	 * @return boolean true if can update client-side attributes.
	 */
	protected function canUpdateClientSide()
	{
		return $this->getControl()->getActiveControl()->canUpdateClientSide();
	}

	/**
	 * Selects an item based on zero-base index on the client side.
	 * @param integer the index (zero-based) of the item to be selected
	 */
	public function setSelectedIndex($index)
	{
		if($this->canUpdateClientSide())
		{
			$this->updateListItems();
			$this->getPage()->getCallbackClient()->select(
					$this->getControl(), 'Index', $index);
		}
	}

	/**
	 * Selects a list of item based on zero-base indices on the client side.
	 * @param array list of index of items to be selected
	 */
	public function setSelectedIndices($indices)
	{
		if($this->canUpdateClientSide())
		{
			$this->updateListItems();
			$n = $this->getControl()->getItemCount();
			$list = array();
			foreach($indices as $index)
			{
				$index = intval($index);
				if($index >= 0 && $index <= $n)
					$list[] = $index;
			}
			if(count($list) > 0)
				$this->getPage()->getCallbackClient()->select(
					$this->getControl(), 'Indices', $list);
		}
	}

	/**
	 * Sets selection by item value on the client side.
	 * @param string the value of the item to be selected.
	 */
	public function setSelectedValue($value)
	{
		if($this->canUpdateClientSide())
		{
			$this->updateListItems();
			$this->getPage()->getCallbackClient()->select(
					$this->getControl(), 'Value', $value);
		}
	}

	/**
	 * Sets selection by a list of item values on the client side.
	 * @param array list of the selected item values
	 */
	public function setSelectedValues($values)
	{
		if($this->canUpdateClientSide())
		{
			$this->updateListItems();
			$list = array();
			foreach($values as $value)
				$list[] = $value;
			if(count($list) > 0)
				$this->getPage()->getCallbackClient()->select(
					$this->getControl(), 'Values', $list);
		}
	}

    /**
     * Clears all existing selections on the client side.
     */
    public function clearSelection()
    {
		if($this->canUpdateClientSide())
		{
			$this->updateListItems();
			$this->getPage()->getCallbackClient()->select($this->getControl(), 'Clear');
		}
    }

	/**
	 * Update the client-side list options.
	 */
	public function updateListItems()
	{
		if($this->canUpdateClientSide())
		{
			$items = $this->getControl()->getItems();
			if($items instanceof TActiveListItemCollection
				&& $items->getListHasChanged())
			{
				$items->updateClientSide();
			}
		}
	}
}

/**
 * TActiveListItemCollection class.
 *
 * Allows TActiveDropDownList and TActiveListBox to add new options
 * during callback response. New options can only be added <b>after</b> the
 * {@link TControl::onLoad OnLoad} event.
 *
 * The {@link getListHasChanged ListHasChanged} property is true when the
 * list items has changed. The control responsible for the list needs to
 * repopulate the client-side options.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveListControlAdapter.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveListItemCollection extends TListItemCollection
{
	/**
	 * @var IActiveControl control instance.
	 */
	private $_control;
	/**
	 * @var boolean true if list items were changed.
	 */
	private $_hasChanged=false;

	/**
	 * @return boolean true if active controls can update client-side and
	 * the onLoad event has already been raised.
	 */
	protected function canUpdateClientSide()
	{
		return $this->getControl()->getActiveControl()->canUpdateClientSide()
				&& $this->getControl()->getHasLoaded();
	}

	/**
	 * @param IActiveControl a active list control.
	 */
	public function setControl(IActiveControl $control)
	{
		$this->_control = $control;
	}

	/**
	 * @return IActiveControl active control using the collection.
	 */
	public function getControl()
	{
		return $this->_control;
	}

	/**
	 * @return boolean true if the list has changed after onLoad event.
	 */
	public function getListHasChanged()
	{
		return $this->_hasChanged;
	}

	/**
	 * Update client-side list items.
	 */
	public function updateClientSide()
	{
		$client = $this->getControl()->getPage()->getCallbackClient();
		$client->setListItems($this->getControl(), $this);
		$this->_hasChanged=false;
	}

	/**
	 * Inserts an item into the collection.
	 * The new option is added on the client-side during callback.
	 * @param integer the location where the item will be inserted.
	 * The current item at the place and the following ones will be moved backward.
	 * @param TListItem the item to be inserted.
	 * @throws TInvalidDataTypeException if the item being inserted is neither a string nor TListItem
	 */
	public function insertAt($index, $value)
	{
		parent::insertAt($index, $value);
		if($this->canUpdateClientSide())
			$this->_hasChanged = true;
	}

	/**
	 * Removes an item from at specified index.
	 * @param int zero based index.
	 */
	public function removeAt($index)
	{
		parent::removeAt($index);
		if($this->canUpdateClientSide())
			$this->_hasChanged = true;
	}
}

?>
