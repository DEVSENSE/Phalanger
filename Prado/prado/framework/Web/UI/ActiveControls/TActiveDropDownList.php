<?php
/**
 * TActiveDropDownList class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveDropDownList.php 2600 2009-01-07 12:58:53Z christophe.boulain $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active list control adapter
 */
Prado::using('System.Web.UI.ActiveControls.TActiveListControlAdapter');

/**
 * TActiveDropDownList class.
 *
 * The active control counter part to drop down list control.
 * The {@link setAutoPostBack AutoPostBack} property is set to true by default.
 * Thus, when the drop down list selection is changed the {@link onCallback OnCallback} event is
 * raised after {@link OnSelectedIndexChanged} event.
 *
 * With {@link TBaseActiveControl::setEnableUpdate() ActiveControl.EnabledUpdate}
 * set to true (default is true), changes to the selection, <b>after</b> OnLoad event has
 * been raised, will be updated.
 * on the client side.
 *
 * List items can be changed dynamically during a callback request.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveDropDownList.php 2600 2009-01-07 12:58:53Z christophe.boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveDropDownList extends TDropDownList implements ICallbackEventHandler, IActiveControl
{
	/**
	 * Creates a new callback control, sets the adapter to
	 * TActiveListControlAdapter. If you override this class, be sure to set the
	 * adapter appropriately by, for example, by calling this constructor.
	 */
	public function __construct()
	{
		parent::__construct();
		$this->setAdapter(new TActiveListControlAdapter($this));
		$this->setAutoPostBack(true);
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
	 * No client class for this control.
	 * This method overrides the parent implementation.
	 * @return null no javascript class name.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TActiveDropDownList';
	}

	/**
	 * Creates a collection object to hold list items. A specialized
	 * TActiveListItemCollection is created to allow the drop down list options
	 * to be added.
	 * This method may be overriden to create a customized collection.
	 * @return TActiveListItemCollection the collection object
	 */
	protected function createListItemCollection()
	{
		$collection  = new TActiveListItemCollection;
		$collection->setControl($this);
		return $collection;
	}

	/**
	 * Override parent implementation, no javascript is rendered here instead
	 * the javascript required for active control is registered in {@link addAttributesToRender}.
	 */
	protected function renderClientControlScript($writer)
	{
	}

	/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control.
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		$writer->addAttribute('id',$this->getClientID());
		if ($this->getAutoPostBack())
			$this->getActiveControl()->registerCallbackClientScript(
				$this->getClientClassName(), $this->getPostBackOptions());
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
	 * Updates the client-side options if the item list has changed after the OnLoad event.
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		$this->getAdapter()->updateListItems();
	}
}

?>
