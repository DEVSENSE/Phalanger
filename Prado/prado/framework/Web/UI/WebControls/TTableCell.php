<?php
/**
 * TTableCell class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTableCell.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TTableCell class.
 *
 * TTableCell displays a table cell on a Web page. Content of the table cell
 * is specified by the {@link setText Text} property. If {@link setText Text}
 * is empty, the body contents enclosed by the table cell component tag are rendered.
 * Note, {@link setText Text} is not HTML-encoded when displayed. So make sure
 * it does not contain dangerous characters.
 *
 * The horizontal and vertical alignments of the contents in the cell
 * are specified via {@link setHorizontalAlign HorizontalAlign} and
 * {@link setVerticalAlign VerticalAlign} properties, respectively.
 *
 * The colspan and rowspan of the cell are specified via {@link setColumnSpan ColumnSpan}
 * and {@link setRowSpan RowSpan} properties. And the {@link setWrap Wrap} property
 * indicates whether the contents in the cell should be wrapped.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTableCell.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTableCell extends TWebControl implements IDataRenderer
{
	/**
	 * @return string tag name for the table cell
	 */
	protected function getTagName()
	{
		return 'td';
	}

	/**
	 * Creates a style object for the control.
	 * This method creates a {@link TTableItemStyle} to be used by the table cell.
	 * @return TStyle control style to be used
	 */
	protected function createStyle()
	{
		return new TTableItemStyle;
	}

	/**
	 * @return string the horizontal alignment of the contents within the table item, defaults to 'NotSet'.
	 */
	public function getHorizontalAlign()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getHorizontalAlign();
		else
			return 'NotSet';
	}

	/**
	 * Sets the horizontal alignment of the contents within the table item.
     * Valid values include 'NotSet', 'Justify', 'Left', 'Right', 'Center'
	 * @param string the horizontal alignment
	 */
	public function setHorizontalAlign($value)
	{
		$this->getStyle()->setHorizontalAlign($value);
	}

	/**
	 * @return string the vertical alignment of the contents within the table item, defaults to 'NotSet'.
	 */
	public function getVerticalAlign()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getVerticalAlign();
		else
			return 'NotSet';
	}

	/**
	 * Sets the vertical alignment of the contents within the table item.
     * Valid values include 'NotSet','Top','Bottom','Middle'
	 * @param string the horizontal alignment
	 */
	public function setVerticalAlign($value)
	{
		$this->getStyle()->setVerticalAlign($value);
	}

	/**
	 * @return integer the columnspan for the table cell, 0 if not set.
	 */
	public function getColumnSpan()
	{
		return $this->getViewState('ColumnSpan', 0);
	}

	/**
	 * Sets the columnspan for the table cell.
	 * @param integer the columnspan for the table cell, 0 if not set.
	 */
	public function setColumnSpan($value)
	{
		$this->setViewState('ColumnSpan', TPropertyValue::ensureInteger($value), 0);
	}

	/**
	 * @return integer the rowspan for the table cell, 0 if not set.
	 */
	public function getRowSpan()
	{
		return $this->getViewState('RowSpan', 0);
	}

	/**
	 * Sets the rowspan for the table cell.
	 * @param integer the rowspan for the table cell, 0 if not set.
	 */
	public function setRowSpan($value)
	{
		$this->setViewState('RowSpan', TPropertyValue::ensureInteger($value), 0);
	}

	/**
	 * @return boolean whether the text content wraps within a table cell. Defaults to true.
	 */
	public function getWrap()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getWrap();
		else
			return true;
	}

	/**
	 * Sets the value indicating whether the text content wraps within a table cell.
	 * @param boolean whether the text content wraps within a table cell.
	 */
	public function setWrap($value)
	{
		$this->getStyle()->setWrap($value);
	}

	/**
	 * @return string the text content of the table cell.
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * Sets the text content of the table cell.
	 * If the text content is empty, body content (child controls) of the cell will be rendered.
	 * @param string the text content
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value,'');
	}

	/**
	 * Returns the text content of the table cell.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getText()}.
	 * @return string the text content of the table cell.
	 * @see getText
	 * @since 3.1.0
	 */
	public function getData()
	{
		return $this->getText();
	}

	/**
	 * Sets the text content of the table cell.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setText()}.
	 * @param string the text content of the table cell.
	 * @see setText
	 * @since 3.1.0
	 */
	public function setData($value)
	{
		$this->setText($value);
	}

	/**
	 * Adds attributes to renderer.
	 * @param THtmlWriter the renderer
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		if(($colspan=$this->getColumnSpan())>0)
			$writer->addAttribute('colspan',"$colspan");
		if(($rowspan=$this->getRowSpan())>0)
			$writer->addAttribute('rowspan',"$rowspan");
	}

	/**
	 * Renders body contents of the table cell.
	 * @param THtmlWriter the writer used for the rendering purpose.
	 */
	public function renderContents($writer)
	{
		if(($text=$this->getText())!=='')
			$writer->write($text);
		else if($this->getHasControls())
			parent::renderContents($writer);
		else
			$writer->write('&nbsp;');
	}
}

