<?php
/**
 * TScaffoldSearch class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Data.ActiveRecord.Scaffold
 */

/**
 * Import the scaffold base.
 */
Prado::using('System.Data.ActiveRecord.Scaffold.TScaffoldBase');

/**
 * TScaffoldSearch provide a simple textbox and a button that is used
 * to perform search on a TScaffoldListView with ID given by {@link setListViewID ListViewID}.
 *
 * The {@link getSearchText SearchText} property is a TTextBox and the
 * {@link getSearchButton SearchButton} property is a TButton with label value "Search".
 *
 * Searchable fields of the Active Record can be restricted by specifying
 * a comma delimited string of allowable fields in the
 * {@link setSearchableFields SearchableFields} property. The default is null,
 * meaning that most text type fields are searched (the default searchable fields
 * are database dependent).
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id$
 * @package System.Data.ActiveRecord.Scaffold
 * @since 3.1
 */
class TScaffoldSearch extends TScaffoldBase
{
	/**
	 * @var TScaffoldListView the scaffold list view.
	 */
	private $_list;

	/**
	 * @return TScaffoldListView the scaffold list view this search box belongs to.
	 */
	protected function getListView()
	{
		if($this->_list===null && ($id = $this->getListViewID()) !== null)
		{
			$this->_list = $this->getParent()->findControl($id);
			if($this->_list ===null)
				throw new TConfigurationException('scaffold_unable_to_find_list_view', $id);
		}
		return $this->_list;
	}

	/**
	 * @param string ID of the TScaffoldListView this search control belongs to.
	 */
	public function setListViewID($value)
	{
		$this->setViewState('ListViewID', $value);
	}

	/**
	 * @return string ID of the TScaffoldListView this search control belongs to.
	 */
	public function getListViewID()
	{
		return $this->getViewState('ListViewID');
	}

	/**
	 * Sets the SearchCondition of the TScaffoldListView as the search terms
	 * given by the text of the search text box.
	 */
	public function bubbleEvent($sender, $param)
	{
		if(strtolower($param->getCommandName())==='search')
		{
			if(($list = $this->getListView()) !== null)
			{
				$list->setSearchCondition($this->createSearchCondition());
				return false;
			}
		}
		$this->raiseBubbleEvent($this, $param);
		return true;
	}

	/**
	 * @return string the search criteria for the search terms in the search text box.
	 */
	protected function createSearchCondition()
	{
		$table = $this->getTableInfo();
		if(strlen($str=$this->getSearchText()->getText()) > 0)
		{
			$builder = $table->createCommandBuilder($this->getRecordFinder()->getDbConnection());
			return $builder->getSearchExpression($this->getFields(), $str);
		}
	}

	/**
	 * @return array list of fields to be searched.
	 */
	protected function getFields()
	{
		if(strlen(trim($str=$this->getSearchableFields()))>0)
			$fields = preg_split('/\s*,\s*/', $str);
		else
			$fields = $this->getTableInfo()->getColumns()->getKeys();
		return $fields;
	}

	/**
	 * @return string comma delimited list of fields that may be searched.
	 */
	public function getSearchableFields()
	{
		return $this->getViewState('SearchableFields','');
	}

	/**
	 * @param string comma delimited list of fields that may be searched.
	 */
	public function setSearchableFields($value)
	{
		$this->setViewState('SearchableFields', $value, '');
	}

	/**
	 * @return TButton button with default label "Search".
	 */
	public function getSearchButton()
	{
		$this->ensureChildControls();
		return $this->getRegisteredObject('_search');
	}

	/**
	 * @return TTextBox search text box.
	 */
	public function getSearchText()
	{
		$this->ensureChildControls();
		return $this->getRegisteredObject('_textbox');
	}
}

