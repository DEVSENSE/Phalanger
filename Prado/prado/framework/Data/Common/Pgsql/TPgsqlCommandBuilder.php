<?php
/**
 * TPgsqlCommandBuilder class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDbCommandBuilder.php 1863 2007-04-12 12:43:49Z wei $
 * @package System.Data.Common
 */

Prado::using('System.Data.Common.TDbCommandBuilder');

/**
 * TPgsqlCommandBuilder provides specifics methods to create limit/offset query commands
 * for Pgsql database.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TDbCommandBuilder.php 1863 2007-04-12 12:43:49Z wei $
 * @package System.Data.Common
 * @since 3.1
 */
class TPgsqlCommandBuilder extends TDbCommandBuilder
{
	/**
	 * Overrides parent implementation. Only column of type text or character (and its variants)
	 * accepts the LIKE criteria.
	 * @param array list of column id for potential search condition.
	 * @param string string of keywords
	 * @return string SQL search condition matching on a set of columns.
	 */
	public function getSearchExpression($fields, $keywords)
	{
		$columns = array();
		foreach($fields as $field)
		{
			if($this->isSearchableColumn($this->getTableInfo()->getColumn($field)))
				$columns[] = $field;
		}
		return parent::getSearchExpression($columns, $keywords);
	}
	/**
	 *
	 * @return boolean true if column can be used for LIKE searching.
	 */
	protected function isSearchableColumn($column)
	{
		$type = strtolower($column->getDbType());
		return $type === 'character varying' || $type === 'varchar' ||
				$type === 'character' || $type === 'char' || $type === 'text';
	}

	/**
	 * Overrides parent implementation to use PostgreSQL's ILIKE instead of LIKE (case-sensitive).
	 * @param string column name.
	 * @param array keywords
	 * @return string search condition for all words in one column.
	 */
	protected function getSearchCondition($column, $words)
	{
		$conditions=array();
		foreach($words as $word)
			$conditions[] = $column.' ILIKE '.$this->getDbConnection()->quoteString('%'.$word.'%');
		return '('.implode(' AND ', $conditions).')';
	}

}

