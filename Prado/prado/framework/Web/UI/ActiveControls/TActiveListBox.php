<?php
/**
 * TActiveListBox class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveListBox.php 2600 2009-01-07 12:58:53Z christophe.boulain $
 * @package System.Web.UI.ActiveControls
 */

/**
 * TActiveListBox class.
 *
 * List items can be added dynamically during a callback request.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveListBox.php 2600 2009-01-07 12:58:53Z christophe.boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveListBox extends TListBox implements IActiveControl, ICallbackEventHandler
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
	 * Javascript client class for this control.
	 * This method overrides the parent implementation.
	 * @return null no javascript class name.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TActiveListBox';
	}

	/**
	 * Sets the selection mode of the list control (Single, Multiple)
	 * on the client-side if the  {@link setEnableUpdate EnableUpdate}
	 * property is set to true.
	 * @param string the selection mode
	 */
	public function setSelectionMode($value)
	{
		parent::setSelectionMode($value);
		$multiple = $this->getIsMultiSelect();
		$id = $this->getUniqueID(); $multi_id = $id.'[]';
		if($multiple)
			$this->getPage()->registerPostDataLoader($multi_id);
		if($this->getActiveControl()->canUpdateClientSide())
		{
			$client = $this->getPage()->getCallbackClient();
			$client->setAttribute($this, 'multiple', $multiple ? 'multiple' : false);
			$client->setAttribute($this, 'name', $multiple ? $multi_id : $id);
			if($multiple)
				$client->addPostDataLoader($multi_id);
		}
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
		$multiple = $this->getIsMultiSelect();
		$id = $this->getUniqueID(); $multi_id = $id.'[]';
		if($multiple)
			$this->getPage()->registerPostDataLoader($multi_id);
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
}

?>
