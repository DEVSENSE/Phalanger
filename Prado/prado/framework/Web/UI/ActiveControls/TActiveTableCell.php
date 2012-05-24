<?php
/**
 * TActiveTableCell class file
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @link http://www.landwehr-software.de/
 * @copyright Copyright &copy; 2009 LANDWEHR Computer und Software GmbH
 * @license http://www.pradosoft.com/license/
 * @package System.Web.UI.ActiveControls
 * @version $Id$
 */

/**
 * Includes the following used classes
 */
Prado::using('System.Web.UI.WebControls.TTableRow');
Prado::using('System.Web.UI.ActiveControls.TActiveControlAdapter');
Prado::using('System.Web.UI.ActiveControls.TCallbackEventParameter');

/**
 * TActiveTableCell class.
 *
 * TActiveTableCell is the active counterpart to the original {@link TTableCell} control
 * and displays a table cell. The horizontal and vertical alignments of the cell
 * are specified via {@link setHorizontalAlign HorizontalAlign} and
 * {@link setVerticalAlign VerticalAlign} properties, respectively.
 *
 * TActiveTableCell allows the contents of the table cell to be changed during callback. When
 * {@link onCellSelected CellSelected} property is set, selecting (clicking on) the cell will
 * perform a callback request causing {@link onCellSelected OnCellSelected} event to be fired.
 *
 * It will also bubble the {@link onCellSelected OnCellSelected} event up to it's parent
 * {@link TActiveTableRow} control which will fire up the event handlers if implemented.
 *
 * TActiveTableCell allows the client-side cell contents to be updated during a
 * callback response by getting a new writer, invoking the render method and flushing the
 * output, similar to a {@link TActivePanel} control.
 * <code>
 * function callback_request($sender, $param)
 * {
 *     $this->active_cell->render($param->getNewWriter());
 * }
 * </code>
 *
 * Please refer to the original documentation of the regular counterpart for usage.
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @version $Id$
 * @since 3.1.9
 */
class TActiveTableCell extends TTableCell implements ICallbackEventHandler, IActiveControl
{

	/**
	* @var TTable parent row control containing the cell
	*/
	private $_row;

	/**
	 * Creates a new callback control, sets the adapter to TActiveControlAdapter.
	 */
	public function __construct()
	{
		parent::__construct();
			$this->setAdapter(new TActiveControlAdapter($this));
	}

	/**
	 * @return TBaseActiveCallbackControl standard callback control options.
	 */
	public function getActiveControl()
	{
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * @return string corresponding javascript class name for this TActiveTableCell.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TActiveTableCell';
	}

	/**
	 * Raises the callback event. This method is required by {@link ICallbackEventHandler}
	 * interface. It will raise {@link onCellSelected OnCellSelected} event with a
	 * {@link TActiveTableCellEventParameter} containing the zero-based index of the
	 * TActiveTableCell.
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the event parameter
	 */
	public function raiseCallbackEvent($param)
	{
		$parameter = new TActiveTableCellEventParameter($this->getResponse(), $param->getCallbackParameter(), $this->getCellIndex());
		$this->onCellSelected($parameter);
		$this->raiseBubbleEvent($this, $parameter);
	}

	/**
	 * This method is invoked when a callback is requested. The method raises
	 * 'OnCellSelected' event to fire up the event handlers. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TActiveTableCellEventParameter event parameter to be passed to the event handlers
	 */
	public function onCellSelected($param)
	{
		$this->raiseEvent('OnCellSelected', $this, $param);
	}

	/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control if the event handler for the
	 * {@link onCellSelected OnCellSelected} event is set.
	 * @param THtmlWriter the writer responsible for rendering
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		$writer->addAttribute('id', $this->getClientID());
		if ($this->hasEventHandler('OnCellSelected'))
			$this->getActiveControl()->registerCallbackClientScript($this->getClientClassName(), $this->getPostBackOptions());
	}

	/**
	 * Renders and replaces the cell's content on the client-side. When render() is
	 * called before the OnPreRender event, such as when render() is called during
	 * a callback event handler, the rendering is defered until OnPreRender event
	 * is raised.
	 * @param THtmlWriter html writer
	 */
	public function render($writer)
	{
		if ($this->getHasPreRendered())
		{
			parent::render($writer);
			if ($this->getActiveControl()->canUpdateClientSide())
				$this->getPage()->getCallbackClient()->replaceContent($this, $writer);
		}
		else {
			$this->getPage()->getAdapter()->registerControlToRender($this, $writer);
			// If we update a TActiveTableCell on callback, we shouldn't update all childs,
			// because the whole content will be replaced by the parent.
			if ($this->getHasControls())
			{
				foreach ($this->findControlsByType('IActiveControl', false) as $control)
					$control->getActiveControl()->setEnableUpdate(false);
			}
		}
	}

	/**
	 * Returns postback specifications for the table cell.
	 * This method is used by framework and control developers.
	 * @return array parameters about how the row defines its postback behavior.
	 */
	protected function getPostBackOptions()
	{
		$options['ID'] = $this->getClientID();
		$options['EventTarget'] = $this->getUniqueID();
		return $options;
	}

	/**
	 * Returns the zero-based index of the TActiveTableCell within the {@link TTableCellCollection}
	 * of the parent {@link TTableRow} control. Raises a {@link TConfigurationException} if the cell
	 * is no member of the cell collection.
	 * @return integer the zero-based index of the cell
	 */
	public function getCellIndex()
	{
		foreach ($this->getRow()->getCells() as $key => $row)
			if ($row == $this) return $key;
		throw new TConfigurationException('tactivetablecell_control_notincollection', get_class($this), $this->getUniqueID());
	}

	/**
	 * Returns the parent {@link TTableRow} control by looping through all parents until a {@link TTableRow}
	 * is found. Raises a {@link TConfigurationException} if no row control is found.
	 * @return TTableRow the parent row control
	 */
	public function getRow()
	{
		if ($this->_row === null)
		{
			$row = $this->getParent();
			while (!($row instanceof TTableRow) && $row !== null)
			{
				$row = $row->getParent();
			}
			if ($row instanceof TTableRow) $this->_row = $row;
			else throw new TConfigurationException('tactivetablecell_control_outoftable', get_class($this), $this->getUniqueID());
		}
		return $this->_row;
	}

}

/**
 * TActiveTableCellEventParameter class.
 *
 * The TActiveTableCellEventParameter provides the parameter passed during the callback
 * requestion in the {@link getCallbackParameter CallbackParameter} property. The
 * callback response content (e.g. new HTML content) must be rendered
 * using an THtmlWriter obtained from the {@link getNewWriter NewWriter}
 * property, which returns a <b>NEW</b> instance of TCallbackResponseWriter.
 *
 * The {@link getSelectedCellIndex SelectedCellIndex} is a zero-based index of the
 * TActiveTableCell , -1 if the cell is not part of the cell collection (this shouldn't
 * happen though since an exception is thrown before).
 *
 * @author LANDWEHR Computer und Software GmbH <programmierung@landwehr-software.de>
 * @package System.Web.UI.ActiveControls
 * @since 3.1.9
 */
class TActiveTableCellEventParameter extends TCallbackEventParameter
{

	/**
	* @var integer the zero-based index of the cell.
	*/
	private $_selectedCellIndex = -1;

	/**
	 * Creates a new TActiveTableRowEventParameter.
	 */
	public function __construct($response, $parameter, $index=-1)
	{
		parent::__construct($response, $parameter);
		$this->_selectedCellIndex = $index;
	}

	/**
	 * Returns the zero-based index of the {@link TActiveTableCell} within the
	 * {@link TTableCellCollection} of the parent {@link TTableRow} control.
	 * @return integer the zero-based index of the cell.
	 */
	public function getSelectedCellIndex()
	{
		return $this->_selectedCellIndex;
	}

}
