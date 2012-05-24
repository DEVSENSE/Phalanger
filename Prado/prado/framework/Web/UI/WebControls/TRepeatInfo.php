<?php
/**
 * IRepeatInfoUser, TRepeatInfo class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TRepeatInfo.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.Web.UI.WebControls.TTable');

/**
 * IRepeatInfoUser interface.
 * This interface must be implemented by classes who want to use {@link TRepeatInfo}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeatInfo.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
interface IRepeatInfoUser
{
	/**
	 * @return boolean whether the repeat user contains footer
	 */
	public function getHasFooter();
	/**
	 * @return boolean whether the repeat user contains header
	 */
	public function getHasHeader();
	/**
	 * @return boolean whether the repeat user contains separators
	 */
	public function getHasSeparators();
	/**
	 * @return integer number of items to be rendered (excluding header, footer and separators)
	 */
	public function getItemCount();
	/**
	 * @param string item type (Header,Footer,Item,AlternatingItem,SelectedItem,EditItem,Separator,Pager)
	 * @param integer zero-based index of the current rendering item.
	 * @return TStyle CSS style used for rendering items (including header, footer and separators)
	 */
	public function generateItemStyle($itemType,$index);
	/**
	 * Renders an item.
	 * @param THtmlWriter writer for the rendering purpose
	 * @param TRepeatInfo repeat information
	 * @param string item type
	 * @param integer zero-based index of the item being rendered
	 */
	public function renderItem($writer,$repeatInfo,$itemType,$index);
}

/**
 * TRepeatInfo class.
 * TRepeatInfo represents repeat information for controls like {@link TCheckBoxList}.
 * The layout of the repeated items is specified via {@link setRepeatLayout RepeatLayout},
 * which can be either Table (default), Flow or Raw.
 * A table layout uses HTML table cells to organize the items while
 * a flow layout uses line breaks to organize the items.
 * The number of columns used to display the items is specified via
 * {@link setRepeatColumns RepeatColumns} property, while the {@link setRepeatDirection RepeatDirection}
 * governs the order of the items being rendered.
 *
 * Note, the Raw layout does not contain any formatting tags and thus ignores
 * the column and repeat direction settings.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeatInfo.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TRepeatInfo extends TComponent
{
	/**
	 * @var string caption of the table used to organize the repeated items
	 */
	private $_caption='';
	/**
	 * @var TTableCaptionAlign alignment of the caption of the table used to organize the repeated items
	 */
	private $_captionAlign=TTableCaptionAlign::NotSet;
	/**
	 * @var integer number of columns that the items should be arranged in
	 */
	private $_repeatColumns=0;
	/**
	 * @var TRepeatDirection direction of the repetition
	 */
	private $_repeatDirection=TRepeatDirection::Vertical;
	/**
	 * @var TRepeatLayout layout of the repeated items
	 */
	private $_repeatLayout=TRepeatLayout::Table;

	/**
	 * @return string caption of the table layout
	 */
	public function getCaption()
	{
		return $this->_caption;
	}

	/**
	 * @param string caption of the table layout
	 */
	public function setCaption($value)
	{
		$this->_caption=$value;
	}

	/**
	 * @return TTableCaptionAlign alignment of the caption of the table layout. Defaults to TTableCaptionAlign::NotSet.
	 */
	public function getCaptionAlign()
	{
		return $this->_captionAlign;
	}

	/**
	 * @return TTableCaptionAlign alignment of the caption of the table layout.
	 */
	public function setCaptionAlign($value)
	{
		$this->_captionAlign=TPropertyValue::ensureEnum($value,'TTableCaptionAlign');
	}

	/**
	 * @return integer the number of columns that the repeated items should be displayed in. Defaults to 0, meaning not set.
	 */
	public function getRepeatColumns()
	{
		return $this->_repeatColumns;
	}

	/**
	 * @param integer the number of columns that the repeated items should be displayed in.
	 */
	public function setRepeatColumns($value)
	{
		if(($value=TPropertyValue::ensureInteger($value))<0)
			throw new TInvalidDataValueException('repeatinfo_repeatcolumns_invalid');
		$this->_repeatColumns=$value;
	}

	/**
	 * @return TRepeatDirection the direction of traversing the repeated items, defaults to TRepeatDirection::Vertical
	 */
	public function getRepeatDirection()
	{
		return $this->_repeatDirection;
	}

	/**
	 * @param TRepeatDirection the direction of traversing the repeated items
	 */
	public function setRepeatDirection($value)
	{
		$this->_repeatDirection=TPropertyValue::ensureEnum($value,'TRepeatDirection');
	}

	/**
	 * @return TRepeatLayout how the repeated items should be displayed, using table or using line breaks. Defaults to TRepeatLayout::Table.
	 */
	public function getRepeatLayout()
	{
		return $this->_repeatLayout;
	}

	/**
	 * @param TRepeatLayout how the repeated items should be displayed, using table or using line breaks.
	 */
	public function setRepeatLayout($value)
	{
		$this->_repeatLayout=TPropertyValue::ensureEnum($value,'TRepeatLayout');
	}

	/**
	 * Renders the repeated items.
	 * @param THtmlWriter writer for the rendering purpose
	 * @param IRepeatInfoUser repeat information user
	 */
	public function renderRepeater($writer, IRepeatInfoUser $user)
	{
		if($this->_repeatLayout===TRepeatLayout::Table)
		{
			$control=new TTable;
			if($this->_caption!=='')
			{
				$control->setCaption($this->_caption);
				$control->setCaptionAlign($this->_captionAlign);
			}
		}
		else if($this->_repeatLayout===TRepeatLayout::Raw)
		{
			$this->renderRawContents($writer,$user);
			return;
		}
		else
			$control=new TWebControl;
		$control->setID($user->getClientID());
		$control->copyBaseAttributes($user);
		if($user->getHasStyle())
			$control->getStyle()->copyFrom($user->getStyle());
		$control->renderBeginTag($writer);
		$writer->writeLine();

		if($this->_repeatDirection===TRepeatDirection::Vertical)
			$this->renderVerticalContents($writer,$user);
		else
			$this->renderHorizontalContents($writer,$user);

		$control->renderEndTag($writer);
	}

	/**
	 * Renders contents in raw format.
	 * @param THtmlWriter writer for the rendering purpose
	 * @param IRepeatInfoUser repeat information user
	 */
	protected function renderRawContents($writer,$user)
	{
		if($user->getHasHeader())
			$user->renderItem($writer,$this,'Header',-1);

		// render items
		$hasSeparators=$user->getHasSeparators();
		$itemCount=$user->getItemCount();
		for($i=0;$i<$itemCount;++$i)
		{
			$user->renderItem($writer,$this,'Item',$i);
			if($hasSeparators && $i!=$itemCount-1)
				$user->renderItem($writer,$this,'Separator',$i);
		}
		if($user->getHasFooter())
			$user->renderItem($writer,$this,'Footer',-1);
	}

	/**
	 * Renders contents in horizontal repeat direction.
	 * @param THtmlWriter writer for the rendering purpose
	 * @param IRepeatInfoUser repeat information user
	 */
	protected function renderHorizontalContents($writer,$user)
	{
		$tableLayout=($this->_repeatLayout===TRepeatLayout::Table);
		$hasSeparators=$user->getHasSeparators();
		$itemCount=$user->getItemCount();
		$columns=$this->_repeatColumns===0?$itemCount:$this->_repeatColumns;
		$totalColumns=$hasSeparators?$columns+$columns:$columns;
		$needBreak=$columns<$itemCount;

		if($user->getHasHeader())
			$this->renderHeader($writer,$user,$tableLayout,$totalColumns,$needBreak);

		// render items
		if($tableLayout)
		{
			$writer->renderBeginTag('tbody');
			$column=0;
			for($i=0;$i<$itemCount;++$i)
			{
				if($column==0)
					$writer->renderBeginTag('tr');
				if(($style=$user->generateItemStyle('Item',$i))!==null)
					$style->addAttributesToRender($writer);
				$writer->renderBeginTag('td');
				$user->renderItem($writer,$this,'Item',$i);
				$writer->renderEndTag();
				$writer->writeLine();
				if($hasSeparators && $i!=$itemCount-1)
				{
					if(($style=$user->generateItemStyle('Separator',$i))!==null)
						$style->addAttributesToRender($writer);
					$writer->renderBeginTag('td');
					$user->renderItem($writer,$this,'Separator',$i);
					$writer->renderEndTag();
					$writer->writeLine();
				}
				$column++;
				if($i==$itemCount-1)
				{
					$restColumns=$columns-$column;
					if($hasSeparators)
						$restColumns=$restColumns?$restColumns+$restColumns+1:1;
					for($j=0;$j<$restColumns;++$j)
						$writer->write("<td></td>\n");
				}
				if($column==$columns || $i==$itemCount-1)
				{
					$writer->renderEndTag();
					$writer->writeLine();
					$column=0;
				}
			}
			$writer->renderEndTag();
		}
		else
		{
			$column=0;
			for($i=0;$i<$itemCount;++$i)
			{
				$user->renderItem($writer,$this,'Item',$i);
				if($hasSeparators && $i!=$itemCount-1)
					$user->renderItem($writer,$this,'Separator',$i);
				$column++;
				if($column==$columns || $i==$itemCount-1)
				{
					if($needBreak)
						$writer->writeBreak();
					$column=0;
				}
				$writer->writeLine();
			}
		}

		if($user->getHasFooter())
			$this->renderFooter($writer,$user,$tableLayout,$totalColumns,$needBreak);
	}

	/**
	 * Renders contents in veritcal repeat direction.
	 * @param THtmlWriter writer for the rendering purpose
	 * @param IRepeatInfoUser repeat information user
	 */
	protected function renderVerticalContents($writer,$user)
	{
		$tableLayout=($this->_repeatLayout===TRepeatLayout::Table);
		$hasSeparators=$user->getHasSeparators();
		$itemCount=$user->getItemCount();
		if($this->_repeatColumns<=1)
		{
			$rows=$itemCount;
			$columns=1;
			$lastColumns=1;
		}
		else
		{
			$columns=$this->_repeatColumns;
			$rows=(int)(($itemCount+$columns-1)/$columns);
			if($rows==0 && $itemCount>0)
				$rows=1;
			if(($lastColumns=$itemCount%$columns)==0)
				$lastColumns=$columns;
		}
		$totalColumns=$hasSeparators?$columns+$columns:$columns;

		if($user->getHasHeader())
			$this->renderHeader($writer,$user,$tableLayout,$totalColumns,false);

		if($tableLayout)
		{
			$writer->renderBeginTag('tbody');
			$renderedItems=0;
			for($row=0;$row<$rows;++$row)
			{
				$index=$row;
				$writer->renderBeginTag('tr');
				for($col=0;$col<$columns;++$col)
				{
					if($renderedItems>=$itemCount)
						break;
					if($col>0)
					{
						$index+=$rows;
						if($col-1>=$lastColumns)
							$index--;
					}
					if($index>=$itemCount)
						continue;
					$renderedItems++;
					if(($style=$user->generateItemStyle('Item',$index))!==null)
						$style->addAttributesToRender($writer);
					$writer->renderBeginTag('td');
					$user->renderItem($writer,$this,'Item',$index);
					$writer->renderEndTag();
					$writer->writeLine();
					if(!$hasSeparators)
						continue;
					if($renderedItems<$itemCount-1)
					{
						if($columns==1)
						{
							$writer->renderEndTag();
							$writer->renderBeginTag('tr');
						}
						if(($style=$user->generateItemStyle('Separator',$index))!==null)
							$style->addAttributesToRender($writer);
						$writer->renderBeginTag('td');
						$user->renderItem($writer,$this,'Separator',$index);
						$writer->renderEndTag();
						$writer->writeLine();
					}
					else if($columns>1)
						$writer->write("<td></td>\n");
				}
				if($row==$rows-1)
				{
					$restColumns=$columns-$lastColumns;
					if($hasSeparators)
						$restColumns+=$restColumns;
					for($col=0;$col<$restColumns;++$col)
						$writer->write("<td></td>\n");
				}
				$writer->renderEndTag();
				$writer->writeLine();
			}
			$writer->renderEndTag();
		}
		else
		{
			$renderedItems=0;
			for($row=0;$row<$rows;++$row)
			{
				$index=$row;
				for($col=0;$col<$columns;++$col)
				{
					if($renderedItems>=$itemCount)
						break;
					if($col>0)
					{
						$index+=$rows;
						if($col-1>=$lastColumns)
							$index--;
					}
					if($index>=$itemCount)
						continue;
					$renderedItems++;
					$user->renderItem($writer,$this,'Item',$index);
					$writer->writeLine();
					if(!$hasSeparators)
						continue;
					if($renderedItems<$itemCount-1)
					{
						if($columns==1)
							$writer->writeBreak();
						$user->renderItem($writer,$this,'Separator',$index);
					}
					$writer->writeLine();
				}
				if($row<$rows-1 || $user->getHasFooter())
					$writer->writeBreak();
			}
		}

		if($user->getHasFooter())
			$this->renderFooter($writer,$user,$tableLayout,$totalColumns,false);

	}

	/**
	 * Renders header.
	 * @param THtmlWriter writer for the rendering purpose
	 * @param IRepeatInfoUser repeat information user
	 * @param boolean whether to render using table layout
	 * @param integer number of columns to be rendered
	 * @param boolean if a line break is needed at the end
	 */
	protected function renderHeader($writer,$user,$tableLayout,$columns,$needBreak)
	{
		if($tableLayout)
		{
			$writer->renderBeginTag('thead');
			$writer->renderBeginTag('tr');
			if($columns>1)
				$writer->addAttribute('colspan',"$columns");
			$writer->addAttribute('scope','col');
			if(($style=$user->generateItemStyle('Header',-1))!==null)
				$style->addAttributesToRender($writer);
			$writer->renderBeginTag('th');
			$user->renderItem($writer,$this,'Header',-1);
			$writer->renderEndTag();
			$writer->renderEndTag();
			$writer->renderEndTag();
		}
		else
		{
			$user->renderItem($writer,$this,'Header',-1);
			if($needBreak)
				$writer->writeBreak();
		}
		$writer->writeLine();
	}

	/**
	 * Renders footer.
	 * @param THtmlWriter writer for the rendering purpose
	 * @param IRepeatInfoUser repeat information user
	 * @param boolean whether to render using table layout
	 * @param integer number of columns to be rendered
	 */
	protected function renderFooter($writer,$user,$tableLayout,$columns)
	{
		if($tableLayout)
		{
			$writer->renderBeginTag('tfoot');
			$writer->renderBeginTag('tr');
			if($columns>1)
				$writer->addAttribute('colspan',"$columns");
			if(($style=$user->generateItemStyle('Footer',-1))!==null)
				$style->addAttributesToRender($writer);
			$writer->renderBeginTag('td');
			$user->renderItem($writer,$this,'Footer',-1);
			$writer->renderEndTag();
			$writer->renderEndTag();
			$writer->renderEndTag();
		}
		else
			$user->renderItem($writer,$this,'Footer',-1);
		$writer->writeLine();
	}
}


/**
 * TRepeatDirection class.
 * TRepeatDirection defines the enumerable type for the possible directions
 * that repeated contents can repeat along
 *
 * The following enumerable values are defined:
 * - Vertical
 * - Horizontal
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeatInfo.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TRepeatDirection extends TEnumerable
{
	const Vertical='Vertical';
	const Horizontal='Horizontal';
}

/**
 * TRepeatLayout class.
 * TRepeatLayout defines the enumerable type for the possible layouts
 * that repeated contents can take.
 *
 * The following enumerable values are defined:
 * - Table: the repeated contents are organized using an HTML table
 * - Flow: the repeated contents are organized using HTML spans and breaks
 * - Raw: the repeated contents are stacked together without any additional decorations
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeatInfo.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TRepeatLayout extends TEnumerable
{
	const Table='Table';
	const Flow='Flow';
	const Raw='Raw';
}

