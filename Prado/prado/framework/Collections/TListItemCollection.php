<?php

/**
 * TListItemCollection class file
 *
 * @author Robin J. Rogge <rojaro@gmail.com>
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2010 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TListControl.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Collections
 */

/**
 * Includes the supporting classes
 */
Prado::using('System.Collections.TList');
Prado::using('System.Web.UI.WebControls.TListItem');

/**
 * TListItemCollection class.
 *
 * TListItemCollection maintains a list of {@link TListItem} for {@link TListControl}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TListControl.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Collections
 * @since 3.0
 */
class TListItemCollection extends TList
{
	/**
	 * Creates a list item object.
	 * This method may be overriden to provide a customized list item object.
	 * @param integer index where the newly created item is to be inserted at.
	 * If -1, the item will be appended to the end.
	 * @return TListItem list item object
	 */
	public function createListItem($index=-1)
	{
		$item=$this->createNewListItem();
		if($index<0)
			$this->add($item);
		else
			$this->insertAt($index,$item);
		return $item;
	}

	/**
	 * @return TListItem new item.
	 */
	protected function createNewListItem($text=null)
	{
		$item =  new TListItem;
		if($text!==null)
			$item->setText($text);
		return $item;
	}

	/**
	 * Inserts an item into the collection.
	 * @param integer the location where the item will be inserted.
	 * The current item at the place and the following ones will be moved backward.
	 * @param TListItem the item to be inserted.
	 * @throws TInvalidDataTypeException if the item being inserted is neither a string nor TListItem
	 */
	public function insertAt($index,$item)
	{
		if(is_string($item))
			$item = $this->createNewListItem($item);
		if(!($item instanceof TListItem))
			throw new TInvalidDataTypeException('listitemcollection_item_invalid',get_class($this));
		parent::insertAt($index,$item);
	}

	/**
	 * Finds the lowest cardinal index of the item whose value is the one being looked for.
	 * @param string the value to be looked for
	 * @param boolean whether to look for disabled items also
	 * @return integer the index of the item found, -1 if not found.
	 */
	public function findIndexByValue($value,$includeDisabled=true)
	{
		$value=TPropertyValue::ensureString($value);
		$index=0;
		foreach($this as $item)
		{
			if($item->getValue()===$value && ($includeDisabled || $item->getEnabled()))
				return $index;
			$index++;
		}
		return -1;
	}

	/**
	 * Finds the lowest cardinal index of the item whose text is the one being looked for.
	 * @param string the text to be looked for
	 * @param boolean whether to look for disabled items also
	 * @return integer the index of the item found, -1 if not found.
	 */
	public function findIndexByText($text,$includeDisabled=true)
	{
		$text=TPropertyValue::ensureString($text);
		$index=0;
		foreach($this as $item)
		{
			if($item->getText()===$text && ($includeDisabled || $item->getEnabled()))
				return $index;
			$index++;
		}
		return -1;
	}

	/**
	 * Finds the item whose value is the one being looked for.
	 * @param string the value to be looked for
	 * @param boolean whether to look for disabled items also
	 * @return TListItem the item found, null if not found.
	 */
	public function findItemByValue($value,$includeDisabled=true)
	{
		if(($index=$this->findIndexByValue($value,$includeDisabled))>=0)
			return $this->itemAt($index);
		else
			return null;
	}

	/**
	 * Finds the item whose text is the one being looked for.
	 * @param string the text to be looked for
	 * @param boolean whether to look for disabled items also
	 * @return TListItem the item found, null if not found.
	 */
	public function findItemByText($text,$includeDisabled=true)
	{
		if(($index=$this->findIndexByText($text,$includeDisabled))>=0)
			return $this->itemAt($index);
		else
			return null;
	}

	/**
	 * Loads state into every item in the collection.
	 * This method should only be used by framework and control developers.
	 * @param array|null state to be loaded.
	 */
	public function loadState($state)
	{
		$this->clear();
		if($state!==null)
			$this->copyFrom($state);
	}

	/**
	 * Saves state of items.
	 * This method should only be used by framework and control developers.
	 * @return array|null the saved state
	 */
	public function saveState()
	{
		return ($this->getCount()>0) ? $this->toArray() : null;
	}
}
