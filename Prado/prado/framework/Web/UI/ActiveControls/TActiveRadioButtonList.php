<?php
/**
 * TActiveRadioButtonList class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveRadioButtonList.php 2573 2008-11-25 12:21:59Z carlgmathisen $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active control adapter and active radio button.
 */
Prado::using('System.Web.UI.ActiveControls.TActiveListControlAdapter');
Prado::using('System.Web.UI.ActiveControls.TActiveRadioButton');

/**
 * TActiveRadioButtonList class.
 *
 * The active control counter part to radio button list control.
 * The {@link setAutoPostBack AutoPostBack} property is set to true by default.
 * Thus, when a radio button is clicked a {@link onCallback OnCallback} event is
 * raised after {@link OnSelectedIndexChanged} event.
 *
 * With {@link TBaseActiveControl::setEnableUpdate() ActiveControl.EnabledUpdate}
 * set to true (default is true), changes to the selection will be updated
 * on the client side.
 *
 * List items can not be changed dynamically during a callback request.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveRadioButtonList.php 2573 2008-11-25 12:21:59Z carlgmathisen $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveRadioButtonList extends TRadioButtonList implements IActiveControl, ICallbackEventHandler
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
	 * Override parent implementation, no javascript is rendered here instead
	 * the javascript required for active control is registered in {@link addAttributesToRender}.
	 */
	protected function renderClientControlScript($writer)
	{
	}

	/**
	 * Creates a control used for repetition (used as a template).
	 * @return TControl the control to be repeated
	 */
	protected function createRepeatedControl()
	{
		$control = new TActiveRadioButton;
		$control->getAdapter()->setBaseActiveControl($this->getActiveControl());
		return $control;
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
}

?>
