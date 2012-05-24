<?php
/**
 * TActiveRepeater class file
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 * @version $Id: TActiveRepeater.php 2707 2009-09-29 10:33:30Z Christophe.Boulain $
 */

/**
 * TActiveRepeater class
 *
 * TActiveRepeater represents a data bound and updatable grid control which is the
 * active counterpart to the original {@link TRepeater} control.
 *
 * This component can be used in the same way as the regular datagrid, the only
 * difference is that the active repeater uses callbacks instead of postbacks
 * for interaction.
 *
 * Please refer to the original documentation of the regular counterparts for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveRepeater extends TRepeater implements IActiveControl, ISurroundable {

/**
 * Creates a new callback control, sets the adapter to
 * TActiveControlAdapter.
 */
	public function __construct() {
		parent::__construct();
		$this->setAdapter(new TActiveControlAdapter($this));
	}

	/**
	 * @return TBaseActiveControl standard active control options.
	 */
	public function getActiveControl() {
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * Sets the data source object associated with the repeater control.
	 * In addition, the render method of all connected pagers is called so they
	 * get updated when the data source is changed. Also the repeater registers
	 * itself for rendering in order to get it's content replaced on client side.
	 * @param Traversable|array|string data source object
	 */
	public function setDataSource($value) {
		parent::setDataSource($value);
		if($this->getActiveControl()->canUpdateClientSide()) {
			$this->renderPager();
			$this->getPage()->getAdapter()->registerControlToRender($this,$this->getResponse()->createHtmlWriter());
		}
	}

	/**
	 * Returns the id of the surrounding container (span).
	 * @return string container id
	 */
	public function getSurroundingTagID() {
		return $this->ClientID.'_Container';
	}

	/**
	 * Renders the repeater.
	 * If the repeater did not pass the prerender phase yet, it will register itself for rendering later.
	 * Else it will call the {@link renderRepeater()} method which will do the rendering of the repeater.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function render($writer) {
		if($this->getHasPreRendered()) {
			$this->renderRepeater($writer);
			if($this->getActiveControl()->canUpdateClientSide()) $this->getPage()->getCallbackClient()->replaceContent($this->getSurroundingTagId(),$writer);
		}
		else {
			$this->getPage()->getAdapter()->registerControlToRender($this,$writer);
		}
	}

	/**
	 * Loops through all {@link TActivePager} on the page and registers the ones which are set to paginate
	 * the repeater for rendering. This is to ensure that the connected pagers are also rendered if the
	 * data source changed.
	 */
	private function renderPager() {
		$pager=$this->getPage()->findControlsByType('TActivePager', false);
		foreach($pager as $item) {
			if($item->ControlToPaginate==$this->ID) {
				$writer=$this->getResponse()->createHtmlWriter();
				$this->getPage()->getAdapter()->registerControlToRender($item,$writer);
			}
		}
	}

	/**
	 * Renders the repeater by writing a span tag with the container id obtained from {@link getSurroundingTagID()}
	 * which will be called by the replacement method of the client script to update it's content.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	private function renderRepeater($writer) {
		$writer->addAttribute('id',$this->getSurroundingTagID());
		$writer->renderBeginTag('span');
		parent::render($writer);
		$writer->renderEndTag();
	}

}

?>
