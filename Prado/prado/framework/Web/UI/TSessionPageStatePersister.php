<?php
/**
 * TSessionPageStatePersister class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSessionPageStatePersister.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI
 */

/**
 * TSessionPageStatePersister class
 *
 * TSessionPageStatePersister implements a page state persistent method based on
 * sessions. Page state are stored in user sessions and therefore, this persister
 * requires session to be started and available.
 *
 * TSessionPageStatePersister keeps limited number of history states in session,
 * mainly to preserve the precious server storage. The number is specified
 * by {@link setHistorySize HistorySize}, which defaults to 10.
 *
 * There are a couple of ways to use TSessionPageStatePersister.
 * One can override the page's {@link TPage::getStatePersister()} method and
 * create a TSessionPageStatePersister instance there.
 * Or one can configure the pages to use TSessionPageStatePersister in page configurations
 * as follows,
 * <code>
 *   <pages StatePersisterClass="System.Web.UI.TSessionPageStatePersister" />
 * </code>
 * The above configuration will affect the pages under the directory containing
 * this configuration and all its subdirectories.
 * To configure individual pages to use TSessionPageStatePersister, use
 * <code>
 *   <pages>
 *     <page id="PageID" StatePersisterClass="System.Web.UI.TSessionPageStatePersister" />
 *   </pages>
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TSessionPageStatePersister.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI
 * @since 3.1
 */
class TSessionPageStatePersister extends TComponent implements IPageStatePersister
{
	const STATE_SESSION_KEY='PRADO_SESSION_PAGESTATE';
	const QUEUE_SESSION_KEY='PRADO_SESSION_STATEQUEUE';

	private $_page;
	private $_historySize=10;

	/**
	 * @param TPage the page that this persister works for
	 */
	public function getPage()
	{
		return $this->_page;
	}

	/**
	 * @param TPage the page that this persister works for.
	 */
	public function setPage(TPage $page)
	{
		$this->_page=$page;
	}

	/**
	 * @return integer maximum number of page states that should be kept in session. Defaults to 10.
	 */
	public function getHistorySize()
	{
		return $this->_historySize;
	}

	/**
	 * @param integer maximum number of page states that should be kept in session
	 * @throws TInvalidDataValueException if the number is smaller than 1.
	 */
	public function setHistorySize($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))>0)
			$this->_historySize=$value;
		else
			throw new TInvalidDataValueException('sessionpagestatepersister_historysize_invalid');
	}
	/**
	 * Saves state in session.
	 * @param mixed state to be stored
	 */
	public function save($state)
	{
		$session=$this->_page->getSession();
		$session->open();
		$data=serialize($state);
		$timestamp=(string)microtime(true);
		$key=self::STATE_SESSION_KEY.$timestamp;
		$session->add($key,$data);
		if(($queue=$session->itemAt(self::QUEUE_SESSION_KEY))===null)
			$queue=array();
		$queue[]=$key;
		if(count($queue)>$this->getHistorySize())
		{
			$expiredKey=array_shift($queue);
			$session->remove($expiredKey);
		}
		$session->add(self::QUEUE_SESSION_KEY,$queue);
		$this->_page->setClientState(TPageStateFormatter::serialize($this->_page,$timestamp));
	}

	/**
	 * Loads page state from session.
	 * @return mixed the restored state
	 * @throws THttpException if page state is corrupted
	 */
	public function load()
	{
		if(($timestamp=TPageStateFormatter::unserialize($this->_page,$this->_page->getRequestClientState()))!==null)
		{
			$session=$this->_page->getSession();
			$session->open();
			$key=self::STATE_SESSION_KEY.$timestamp;
			if(($data=$session->itemAt($key))!==null)
				return unserialize($data);
		}
		throw new THttpException(400,'sessionpagestatepersister_pagestate_corrupted');
	}
}

