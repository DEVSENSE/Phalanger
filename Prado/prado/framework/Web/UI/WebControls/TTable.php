<?php
/**
 * TTable and TTableRowCollection class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTable.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TTableRow class
 */
Prado::using('System.Web.UI.WebControls.TTableRow');

/**
 * TTable class
 *
 * TTable displays an HTML table on a Web page.
 *
 * A table may have {@link setCaption Caption}, whose alignment is specified
 * via {@link setCaptionAlign CaptionAlign}. The table cellpadding and cellspacing
 * are specified via {@link setCellPadding CellPadding} and {@link setCellSpacing CellSpacing}
 * properties, respectively. The {@link setGridLines GridLines} specifies how
 * the table should display its borders. The horizontal alignment of the table
 * content can be specified via {@link setHorizontalAlign HorizontalAlign},
 * and {@link setBackImageUrl BackImageUrl} can assign a background image to the table.
 *
 * A TTable maintains a list of {@link TTableRow} controls in its
 * {@link getRows Rows} property. Each {@link TTableRow} represents
 * an HTML table row.
 *
 * To populate the table {@link getRows Rows}, you may either use control template
 * or dynamically create {@link TTableRow} in code.
 * In template, do as follows to create the table rows and cells,
 * <code>
 *   <com:TTable>
 *     <com:TTableRow>
 *       <com:TTableCell Text="content" />
 *       <com:TTableCell Text="content" />
 *     </com:TTableRow>
 *     <com:TTableRow>
 *       <com:TTableCell Text="content" />
 *       <com:TTableCell Text="content" />
 *     </com:TTableRow>
 *   </com:TTable>
 * </code>
 * The above can also be accomplished in code as follows,
 * <code>
 *   $table=new TTable;
 *   $row=new TTableRow;
 *   $cell=new TTableCell; $cell->Text="content"; $row->Cells->add($cell);
 *   $cell=new TTableCell; $cell->Text="content"; $row->Cells->add($cell);
 *   $table->Rows->add($row);
 *   $row=new TTableRow;
 *   $cell=new TTableCell; $cell->Text="content"; $row->Cells->add($cell);
 *   $cell=new TTableCell; $cell->Text="content"; $row->Cells->add($cell);
 *   $table->Rows->add($row);
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTable.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTable extends TWebControl
{
	/**
	 * @return string tag name for the table
	 */
	protected function getTagName()
	{
		return 'table';
	}

	/**
	 * Adds object parsed from template to the control.
	 * This method adds only {@link TTableRow} objects into the {@link getRows Rows} collection.
	 * All other objects are ignored.
	 * @param mixed object parsed from template
	 */
	public function addParsedObject($object)
	{
		if($object instanceof TTableRow)
			$this->getRows()->add($object);
	}

	/**
	 * Creates a style object for the control.
	 * This method creates a {@link TTableStyle} to be used by the table.
	 * @return TTableStyle control style to be used
	 */
	protected function createStyle()
	{
		return new TTableStyle;
	}

	/**
	 * Adds attributes to renderer.
	 * @param THtmlWriter the renderer
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		$border=0;
		if($this->getHasStyle())
		{
			if($this->getGridLines()!==TTableGridLines::None)
			{
				if(($border=$this->getBorderWidth())==='')
					$border=1;
				else
					$border=(int)$border;
			}
		}
		$writer->addAttribute('border',"$border");
	}

	/**
	 * Creates a control collection object that is to be used to hold child controls
	 * @return TTableRowCollection control collection
	 * @see getControls
	 */
	protected function createControlCollection()
	{
		return new TTableRowCollection($this);
	}

	/**
	 * @return TTableRowCollection list of {@link TTableRow} controls
	 */
	public function getRows()
	{
		return $this->getControls();
	}

	/**
	 * @return string table caption
	 */
	public function getCaption()
	{
		return $this->getViewState('Caption','');
	}

	/**
	 * @param string table caption
	 */
	public function setCaption($value)
	{
		$this->setViewState('Caption',$value,'');
	}

	/**
	 * @return TTableCaptionAlign table caption alignment. Defaults to TTableCaptionAlign::NotSet.
	 */
	public function getCaptionAlign()
	{
		return $this->getViewState('CaptionAlign',TTableCaptionAlign::NotSet);
	}

	/**
	 * @param TTableCaptionAlign table caption alignment.
	 */
	public function setCaptionAlign($value)
	{
		$this->setViewState('CaptionAlign',TPropertyValue::ensureEnum($value,'TTableCaptionAlign'),TTableCaptionAlign::NotSet);
	}

	/**
	 * @return integer the cellspacing for the table. Defaults to -1, meaning not set.
	 */
	public function getCellSpacing()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getCellSpacing();
		else
			return -1;
	}

	/**
	 * @param integer the cellspacing for the table. Defaults to -1, meaning not set.
	 */
	public function setCellSpacing($value)
	{
		$this->getStyle()->setCellSpacing($value);
	}

	/**
	 * @return integer the cellpadding for the table. Defaults to -1, meaning not set.
	 */
	public function getCellPadding()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getCellPadding();
		else
			return -1;
	}

	/**
	 * @param integer the cellpadding for the table. Defaults to -1, meaning not set.
	 */
	public function setCellPadding($value)
	{
		$this->getStyle()->setCellPadding($value);
	}

	/**
	 * @return THorizontalAlign the horizontal alignment of the table content. Defaults to THorizontalAlign::NotSet.
	 */
	public function getHorizontalAlign()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getHorizontalAlign();
		else
			return THorizontalAlign::NotSet;
	}

	/**
	 * @param THorizontalAlign the horizontal alignment of the table content.
	 */
	public function setHorizontalAlign($value)
	{
		$this->getStyle()->setHorizontalAlign($value);
	}

	/**
	 * @return TTableGridLines the grid line setting of the table. Defaults to TTableGridLines::None.
	 */
	public function getGridLines()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getGridLines();
		else
			return TTableGridLines::None;
	}

	/**
	 * @param TTableGridLines the grid line setting of the table
	 */
	public function setGridLines($value)
	{
		$this->getStyle()->setGridLines($value);
	}

	/**
	 * @return string the URL of the background image for the table
	 */
	public function getBackImageUrl()
	{
		if($this->getHasStyle())
			return $this->getStyle()->getBackImageUrl();
		else
			return '';
	}

	/**
	 * Sets the URL of the background image for the table
	 * @param string the URL
	 */
	public function setBackImageUrl($value)
	{
		$this->getStyle()->setBackImageUrl($value);
	}

	/**
	 * Renders the openning tag for the table control which will render table caption if present.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function renderBeginTag($writer)
	{
		parent::renderBeginTag($writer);
		if(($caption=$this->getCaption())!=='')
		{
			if(($align=$this->getCaptionAlign())!==TTableCaptionAlign::NotSet)
				$writer->addAttribute('align',strtolower($align));
			$writer->renderBeginTag('caption');
			$writer->write($caption);
			$writer->renderEndTag();
		}
	}

	/**
	 * Renders body contents of the table.
	 * @param THtmlWriter the writer used for the rendering purpose.
	 */
	public function renderContents($writer)
	{
		if($this->getHasControls())
		{
			$renderTableSection=false;
			foreach($this->getControls() as $row)
			{
				if($row->getTableSection()!==TTableRowSection::Body)
				{
					$renderTableSection=true;
					break;
				}
			}
			if($renderTableSection)
			{
				$currentSection=TTableRowSection::Header;
				$writer->writeLine();
				foreach($this->getControls() as $index=>$row)
				{
					if(($section=$row->getTableSection())===$currentSection)
					{
						if($index===0 && $currentSection===TTableRowSection::Header)
							$writer->renderBeginTag('thead');
					}
					else
					{
						if($currentSection===TTableRowSection::Header)
						{
							if($index>0)
								$writer->renderEndTag();
							if($section===TTableRowSection::Body)
								$writer->renderBeginTag('tbody');
							else
								$writer->renderBeginTag('tfoot');
							$currentSection=$section;
						}
						else if($currentSection===TTableRowSection::Body)
						{
							$writer->renderEndTag();
							if($section===TTableRowSection::Footer)
								$writer->renderBeginTag('tfoot');
							else
								throw new TConfigurationException('table_tablesection_outoforder');
							$currentSection=$section;
						}
						else // Footer
							throw new TConfigurationException('table_tablesection_outoforder');
					}
					$row->renderControl($writer);
					$writer->writeLine();
				}
				$writer->renderEndTag();
			}
			else
			{
				$writer->writeLine();
				foreach($this->getControls() as $row)
				{
					$row->renderControl($writer);
					$writer->writeLine();
				}
			}
		}
	}
}


/**
 * TTableRowCollection class.
 *
 * TTableRowCollection is used to maintain a list of rows belong to a table.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTable.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTableRowCollection extends TControlCollection
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by performing additional
	 * operations for each newly added table row.
	 * @param integer the speicified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is not a TTableRow object.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TTableRow)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('tablerowcollection_tablerow_required');
	}
}


/**
 * TTableCaptionAlign class.
 * TTableCaptionAlign defines the enumerable type for the possible alignments
 * that a table caption can take.
 *
 * The following enumerable values are defined:
 * - NotSet: alignment not specified
 * - Top: top aligned
 * - Bottom: bottom aligned
 * - Left: left aligned
 * - Right: right aligned
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTable.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TTableCaptionAlign extends TEnumerable
{
	const NotSet='NotSet';
	const Top='Top';
	const Bottom='Bottom';
	const Left='Left';
	const Right='Right';
}

