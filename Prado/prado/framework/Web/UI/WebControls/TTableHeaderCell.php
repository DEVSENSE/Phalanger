<?php
/**
 * TTableHeaderCell class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTableHeaderCell.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TTableCell class
 */
Prado::using('System.Web.UI.WebControls.TTableCell');


/**
 * TTableHeaderCell class.
 *
 * TTableHeaderCell displays a table header cell on a Web page.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTableHeaderCell.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTableHeaderCell extends TTableCell
{
	/**
	 * @return string tag name for the table header cell
	 */
	protected function getTagName()
	{
		return 'th';
	}

	/**
	 * Adds attributes to renderer.
	 * @param THtmlWriter the renderer
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		if(($scope=$this->getScope())!==TTableHeaderScope::NotSet)
			$writer->addAttribute('scope',$scope===TTableHeaderScope::Row?'row':'col');
		if(($text=$this->getAbbreviatedText())!=='')
			$writer->addAttribute('abbr',$text);
		if(($text=$this->getCategoryText())!=='')
			$writer->addAttribute('axis',$text);
	}

	/**
	 * @return TTableHeaderScope the scope of the cells that the header cell applies to. Defaults to TTableHeaderScope::NotSet.
	 */
	public function getScope()
	{
		return $this->getViewState('Scope',TTableHeaderScope::NotSet);
	}

	/**
	 * @param TTableHeaderScope the scope of the cells that the header cell applies to.
	 */
	public function setScope($value)
	{
		$this->setViewState('Scope',TPropertyValue::ensureEnum($value,'TTableHeaderScope'),TTableHeaderScope::NotSet);
	}

	/**
	 * @return string  the abbr attribute of the HTML th element
	 */
	public function getAbbreviatedText()
	{
		return $this->getViewState('AbbreviatedText','');
	}

	/**
	 * @param string  the abbr attribute of the HTML th element
	 */
	public function setAbbreviatedText($value)
	{
		$this->setViewState('AbbreviatedText',$value,'');
	}

	/**
	 * @return string the axis attribute of the HTML th element
	 */
	public function getCategoryText()
	{
		return $this->getViewState('CategoryText','');
	}

	/**
	 * @param string the axis attribute of the HTML th element
	 */
	public function setCategoryText($value)
	{
		$this->setViewState('CategoryText',$value,'');
	}
}


/**
 * TTableHeaderScope class.
 * TTableHeaderScope defines the enumerable type for the possible table scopes that a table header is associated with.
 *
 * The following enumerable values are defined:
 * - NotSet: the scope is not specified
 * - Row: the scope is row-wise
 * - Column: the scope is column-wise
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTableHeaderCell.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TTableHeaderScope extends TEnumerable
{
	const NotSet='NotSet';
	const Row='Row';
	const Column='Column';
}

