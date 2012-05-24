<?php
/**
 * TDummyDataSource, TDummyDataSourceIterator classes
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDummyDataSource.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Collections
 */

/**
 * TDummyDataSource class
 *
 * TDummyDataSource implements a dummy data collection with a specified number
 * of dummy data items. The number of virtual items can be set via
 * {@link setCount Count} property. You can traverse it using <b>foreach</b>
 * PHP statement like the following,
 * <code>
 * foreach($dummyDataSource as $dataItem)
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDummyDataSource.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Collections
 * @since 3.0
 */
class TDummyDataSource extends TComponent implements IteratorAggregate, Countable
{
	private $_count;

	/**
	 * Constructor.
	 * @param integer number of (virtual) items in the data source.
	 */
	public function __construct($count)
	{
		$this->_count=$count;
	}

	/**
	 * @return integer number of (virtual) items in the data source.
	 */
	public function getCount()
	{
		return $this->_count;
	}

	/**
	 * @return Iterator iterator
	 */
	public function getIterator()
	{
		return new TDummyDataSourceIterator($this->_count);
	}

	/**
	 * Returns the number of (virtual) items in the data source.
	 * This method is required by Countable interface.
	 * @return integer number of (virtual) items in the data source.
	 */
	public function count()
	{
		return $this->getCount();
	}
}

/**
 * TDummyDataSourceIterator class
 *
 * TDummyDataSourceIterator implements Iterator interface.
 *
 * TDummyDataSourceIterator is used by {@link TDummyDataSource}.
 * It allows TDummyDataSource to return a new iterator
 * for traversing its dummy items.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDummyDataSource.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Collections
 * @since 3.0
 */
class TDummyDataSourceIterator implements Iterator
{
	private $_index;
	private $_count;

	/**
	 * Constructor.
	 * @param integer number of (virtual) items in the data source.
	 */
	public function __construct($count)
	{
		$this->_count=$count;
		$this->_index=0;
	}

	/**
	 * Rewinds internal array pointer.
	 * This method is required by the interface Iterator.
	 */
	public function rewind()
	{
		$this->_index=0;
	}

	/**
	 * Returns the key of the current array item.
	 * This method is required by the interface Iterator.
	 * @return integer the key of the current array item
	 */
	public function key()
	{
		return $this->_index;
	}

	/**
	 * Returns the current array item.
	 * This method is required by the interface Iterator.
	 * @return mixed the current array item
	 */
	public function current()
	{
		return null;
	}

	/**
	 * Moves the internal pointer to the next array item.
	 * This method is required by the interface Iterator.
	 */
	public function next()
	{
		$this->_index++;
	}

	/**
	 * Returns whether there is an item at current position.
	 * This method is required by the interface Iterator.
	 * @return boolean
	 */
	public function valid()
	{
		return $this->_index<$this->_count;
	}
}

