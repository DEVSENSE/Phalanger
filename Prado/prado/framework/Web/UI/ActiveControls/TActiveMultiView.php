<?php
/**
 * TActiveMultiView class file
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @link http://www.landwehr-software.de/
 * @copyright Copyright &copy; 2009 LANDWEHR Computer und Software GmbH
 * @license http://www.pradosoft.com/license/
 * @package System.Web.UI.ActiveControls
 */

/**
 * Includes the following used classes
 */
Prado::using('System.Web.UI.WebControls.TMultiView');

/**
 * TActiveMultiView class.
 *
 * TActiveMultiView is the active counterpart to the original {@link TMultiView} control.
 * It re-renders on Callback when {@link setActiveView ActiveView} or
 * {@link setActiveViewIndex ActiveViewIndex} is called.
 *
 * Please refer to the original documentation of the regular counterpart for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.6
 */
class TActiveMultiView extends TMultiView implements IActiveControl
{
	/**
	* Creates a new callback control, sets the adapter to
	* TActiveControlAdapter.
	*/
	public function __construct()
	{
		parent::__construct();
		$this->setAdapter(new TActiveControlAdapter($this));
	}

	/**
	* @return TBaseActiveControl standard active control options.
	*/
	public function getActiveControl()
	{
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	* Returns the id of the surrounding container (span).
	* @return string container id
	*/
	protected function getContainerID()
	{
		return $this->ClientID.'_Container';
	}

	/**
	* Renders the TActiveMultiView.
	* If the MutliView did not pass the prerender phase yet, it will register itself for rendering later.
	* Else it will call the {@link renderMultiView()} method which will do the rendering of the MultiView.
	* @param THtmlWriter writer for the rendering purpose
	*/
	public function render($writer)
	{
		if($this->getHasPreRendered()) {
			$this->renderMultiView($writer);
			if($this->getActiveControl()->canUpdateClientSide())
				$this->getPage()->getCallbackClient()->replaceContent($this->getContainerID(),$writer);
		}
		else
			$this->getPage()->getAdapter()->registerControlToRender($this,$writer);
	}

	/**
	* Renders the TActiveMultiView by writing a span tag with the container id obtained from {@link getContainerID()}
	* which will be called by the replacement method of the client script to update it's content.
	* @param $writer THtmlWriter writer for the rendering purpose
	*/
	protected function renderMultiView($writer)
	{
		$writer->addAttribute('id', $this->getContainerID());
		$writer->renderBeginTag('span');
		parent::render($writer);
		$writer->renderEndTag();
	}

	/**
	* @param integer the zero-based index of the current view in the view collection. -1 if no active view.
	* @throws TInvalidDataValueException if the view index is invalid
	*/
	public function setActiveViewIndex($value)
	{
		parent::setActiveViewIndex($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getAdapter()->registerControlToRender($this,$this->getResponse()->createHtmlWriter());
	}

	/**
	* @param TView the view to be activated
	* @throws TInvalidOperationException if the view is not in the view collection
	*/
	public function setActiveView($value)
	{
		parent::setActiveView($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getAdapter()->registerControlToRender($this,$this->getResponse()->createHtmlWriter());
	}
}
