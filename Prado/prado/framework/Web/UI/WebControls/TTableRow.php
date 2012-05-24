<?php
/**
 * TTableRow and TTableCellCollection class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTableRow.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TTableCell class
 */
Prado::using('System.Web.UI.WebControls.TTableCell');

/**
 * TTableRow class.
 *
 * TTableRow displays a table row. The table cells in the row can be accessed
 * via {@link getCells Cells}. The horizontal and vertical alignments of the row
 * are specified via {@link setHorizontalAlign HorizontalAlign} and
 * {@link setVerticalAlign VerticalAlign} properties, respectively.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTableRow.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTableRow extends TWebControl
{
	/**
	 * @return string tag name for the table
	 */
	protected function getTagName()
	{
		return 'tr';
	}

	/**
	 * Adds object parsed from template to the control.
	 * This method adds only {@link TTableCell} objects into the {@link getCells Cells} collection.
	 * All other objects are ignored.
	 * @param mixed object parsed from template
	 */
	public function addParsedObject($object)
	{
		if($object instanceof TTableCell)
			$this->getCells()->add($object);
	}

	/**
	 * Creates a style object for the control.
	 * This method creates a {@link TTableItemStyle} to be used by the table row.
	 * @return TStyle control style to be used
	 */
	protected function createStyle()
	{
		return new TTableItemStyle;
	}

	/**
	 * Creates a control collection object that is to be used to hold child controls
	 * @return TTableCellCollection control collection
	 * @see getControls
	 */
	protected function createControlCollection()
	{
		return new TTableCellCollection($this);
	}

	/**
	 * @return TTableCellCollection list of {@link TTableCell} controls
	 */
	public function getCells()
	{
		return $this->getControls();
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
	 * @return TTableRowSection location of a row in a table. Defaults to TTableRowSection::Body.
	 */
	public function getTableSection()
	{
		return $this->getViewState('TableSection',TTableRowSection::Body);
	}

	/**
	 * @param TTableRowSection location of a row in a table.
	 */
	public function setTableSection($value)
	{
		$this->setViewState('TableSection',TPropertyValue::ensureEnum($value,'TTableRowSection'),TTableRowSection::Body);
	}

	/**
	 * Renders body contents of the table row
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function renderContents($writer)
	{
		if($this->getHasControls())
		{
			$writer->writeLine();
			foreach($this->getControls() as $cell)
			{
				$cell->renderControl($writer);
				$writer->writeLine();
			}
		}
	}
}

/**
 * TTableCellCollection class.
 *
 * TTableCellCollection is used to maintain a list of cells belong to a table row.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTableRow.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTableCellCollection extends TControlCollection
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by performing additional
	 * operations for each newly added table cell.
	 * @param integer the speicified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a TTableCell object.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TTableCell)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('tablecellcollection_tablecell_required');
	}
}


/**
 * TTableRowSection class.
 * TTableRowSection defines the enumerable type for the possible table sections
 * that a {@link TTableRow} can be within.
 *
 * The following enumerable values are defined:
 * - Header: in table header
 * - Body: in table body
 * - Footer: in table footer
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTableRow.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TTableRowSection extends TEnumerable
{
	const Header='Header';
	const Body='Body';
	const Footer='Footer';
}

