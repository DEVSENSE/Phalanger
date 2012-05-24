<?php
/**
 * TPageStatePersister class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPageStatePersister.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI
 */

/**
 * TPageStatePersister class
 *
 * TPageStatePersister implements a page state persistent method based on
 * form hidden fields.
 *
 * Since page state can be very big for complex pages, consider using
 * alternative persisters, such as {@link TSessionPageStatePersister},
 * which store page state on the server side and thus reduce the network
 * traffic for transmitting bulky page state.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TPageStatePersister.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI
 * @since 3.0
 */
class TPageStatePersister extends TComponent implements IPageStatePersister
{
	private $_page;

	/**
	 * @return TPage the page that this persister works for
	 */
	public function getPage()
	{
		return $this->_page;
	}

	/**
	 * @param TPage the page that this persister works for
	 */
	public function setPage(TPage $page)
	{
		$this->_page=$page;
	}

	/**
	 * Saves state in hidden fields.
	 * @param mixed state to be stored
	 */
	public function save($state)
	{
		$this->_page->setClientState(TPageStateFormatter::serialize($this->_page,$state));
	}

	/**
	 * Loads page state from hidden fields.
	 * @return mixed the restored state
	 * @throws THttpException if page state is corrupted
	 */
	public function load()
	{
		if(($data=TPageStateFormatter::unserialize($this->_page,$this->_page->getRequestClientState()))!==null)
			return $data;
		else
			throw new THttpException(400,'pagestatepersister_pagestate_corrupted');
	}
}

