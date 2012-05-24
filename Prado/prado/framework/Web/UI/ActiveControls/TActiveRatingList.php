<?php
/**
 * TActiveRatingList class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @author Bradley Booms <bradley[dot]booms[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Web.UI.ActiveControls
 */

/**
 * TActiveRatingList Class
 *
 * Displays clickable images that represent a TRadioButtonList
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @author Bradley Booms <bradley[dot]booms[at]gmail[dot]com>
 * @version $Id$
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveRatingList extends TRatingList implements IActiveControl, ICallbackEventHandler
{
	/**
	 * Creates a new callback control, sets the adapter to
	 * TActiveListControlAdapter. If you override this class, be sure to set the
	 * adapter appropriately by, for example, by calling this constructor.
	 */
	public function __construct()
	{
		$this->setAdapter(new TActiveListControlAdapter($this));
		$this->setAutoPostBack(true);
		parent::__construct();
	}

	/**
	 * @return TBaseActiveCallbackControl standard callback control options.
	 */
	public function getActiveControl()
	{
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * @return TCallbackClientSide client side request options.
	 */
	public function getClientSide()
	{
		return $this->getAdapter()->getBaseActiveControl()->getClientSide();
	}

	/**
	 * Raises the callback event. This method is required by {@link
	 * ICallbackEventHandler} interface.
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the event parameter
	 */
 	public function raiseCallbackEvent($param)
	{
		$this->onCallback($param);
	}

	/**
	 * This method is invoked when a callback is requested. The method raises
	 * 'OnCallback' event to fire up the event handlers. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onCallback($param)
	{
		$this->raiseEvent('OnCallback', $this, $param);
	}

	/**
	 * @param boolean whether the items in the column can be edited
	 */
	public function setReadOnly($value)
	{
		parent::setReadOnly($value);
		$value = $this->getReadOnly();
		$this->callClientFunction('setReadOnly',$value);
	}

	/**
	 * @param float rating value, also sets the selected Index
	 */
	public function setRating($value)
	{
		parent::setRating($value);
		$value = $this->getRating();
		$this->callClientFunction('setRating',$value);
	}

	/**
	 * Calls the client-side static method for this control class.
	 * @param string static method name
	 * @param mixed method parmaeter
	 */
	protected function callClientFunction($func,$value)
	{
		if($this->getActiveControl()->canUpdateClientSide())
		{
			$client = $this->getPage()->getCallbackClient();
			$code = parent::getClientClassName().'.'.$func;
			$client->callClientFunction($code,array($this,$value));
		}
	}

	/**
	 * @param string caption text
	 */
	public function setCaption($value)
	{
		parent::setCaption($value);
		// if it's an active control, this should not be needed. 
		$this->callClientFunction('setCaption',$value);
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TActiveRatingList';
	}
}

