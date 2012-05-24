<?php

/**
 * TOracleCommandBuilder class file.
 *
 * @author Marcos Nobre <marconobre[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TOracleCommandBuilder.php 2651 2009-05-11 08:48:37Z Christophe.Boulain $
 * @package System.Data.Common
 */

Prado :: using('System.Data.Common.TDbCommandBuilder');

/**
 * TOracleCommandBuilder provides specifics methods to create limit/offset query commands
 * for Oracle database.
 *
 * @author Marcos Nobre <marconobre[at]gmail[dot]com>
 * @version $Id: TOracleCommandBuilder.php 2651 2009-05-11 08:48:37Z Christophe.Boulain $
 * @package System.Data.Common
 * @since 3.1
 */
class TOracleCommandBuilder extends TDbCommandBuilder {

	/**
	 * Overrides parent implementation. Only column of type text or character (and its variants)
	 * accepts the LIKE criteria.
	 * @param array list of column id for potential search condition.
	 * @param string string of keywords
	 * @return string SQL search condition matching on a set of columns.
	 */
	public function getSearchExpression($fields, $keywords) {
		$columns = array ();
		foreach ($fields as $field) {
			if ($this->isSearchableColumn($this->getTableInfo()->getColumn($field)))
				$columns[] = $field;
		}
		return parent :: getSearchExpression($columns, $keywords);
	}
	/**
	 *
	 * @return boolean true if column can be used for LIKE searching.
	 */
	protected function isSearchableColumn($column) {
		$type = strtolower($column->getDbType());
		return $type === 'character varying' || $type === 'varchar2' || $type === 'character' || $type === 'char' || $type === 'text';
	}

	/**
	 * Overrides parent implementation to use PostgreSQL's ILIKE instead of LIKE (case-sensitive).
	 * @param string column name.
	 * @param array keywords
	 * @return string search condition for all words in one column.
	 */
	/*
	*
	*	how Oracle don't implements ILIKE, this method won't be overrided
	*
	protected function getSearchCondition($column, $words)
	{
		$conditions=array();
		foreach($words as $word)
			$conditions[] = $column.' LIKE '.$this->getDbConnection()->quoteString('%'.$word.'%');
		return '('.implode(' AND ', $conditions).')';
	}
	*/

	/**
	 * Overrides parent implementation to use Oracle way of get paginated RecordSet instead of using LIMIT sql clause.
	 * @param string SQL query string.
	 * @param integer maximum number of rows, -1 to ignore limit.
	 * @param integer row offset, -1 to ignore offset.
	 * @return string SQL with limit and offset in Oracle way.
	 */
	public function applyLimitOffset($sql, $limit = -1, $offset = -1) {
		if ((int) $limit <= 0 && (int) $offset <= 0)
			return $sql;

		$pradoNUMLIN = 'pradoNUMLIN';
		$fieldsALIAS = 'xyz';

		$nfimDaSQL = strlen($sql);
		$nfimDoWhere = (strpos($sql, 'ORDER') !== false ? strpos($sql, 'ORDER') : $nfimDaSQL);
		$niniDoSelect = strpos($sql, 'SELECT') + 6;
		$nfimDoSelect = (strpos($sql, 'FROM') !== false ? strpos($sql, 'FROM') : $nfimDaSQL);

		$WhereInSubSelect="";
		if(strpos($sql, 'WHERE')!==false)
			$WhereInSubSelect = "WHERE " .substr($sql, strpos($sql, 'WHERE')+5, $nfimDoWhere - $niniDoWhere);

		$sORDERBY = '';
		if (stripos($sql, 'ORDER') !== false) {
			$p = stripos($sql, 'ORDER');
			$sORDERBY = substr($sql, $p +8);
		}

		$fields = substr($sql, 0, $nfimDoSelect);
		$fields = trim(substr($fields, $niniDoSelect));
		$aliasedFields = ', ';

		if (trim($fields) == '*') {
			$aliasedFields = ", {$fieldsALIAS}.{$fields}";
			$fields = '';
			$arr = $this->getTableInfo()->getColumns();
			foreach ($arr as $field) {
				$fields .= strtolower($field->getColumnName()) . ', ';
			}
			$fields = str_replace('"', '', $fields);
			$fields = trim($fields);
			$fields = substr($fields, 0, strlen($fields) - 1);
		} else {
			if (strpos($fields, ',') !== false) {
				$arr = $this->getTableInfo()->getColumns();
				foreach ($arr as $field) {
					$field = strtolower($field);
					$existAS = str_ireplace(' as ', '-as-', $field);
					if (strpos($existAS, '-as-') === false)
						$aliasedFields .= "{$fieldsALIAS}." . trim($field) . ", ";
					else
						$aliasedFields .= "{$field}, ";
				}
				$aliasedFields = trim($aliasedFields);
				$aliasedFields = substr($aliasedFields, 0, strlen($aliasedFields) - 1);
			}
		}
		if ($aliasedFields == ', ')
			$aliasedFields = " , $fieldsALIAS.* ";

		/* ************************
		$newSql = " SELECT $fields FROM ".
				  "(					".
				  "		SELECT rownum as {$pradoNUMLIN} {$aliasedFields} FROM ".
				  " ($sql) {$fieldsALIAS} WHERE rownum <= {$limit} ".
				  ") WHERE {$pradoNUMLIN} >= {$offset} ";
		
		************************* */
		$offset=(int)$offset;
		$toReg = $offset + $limit ;
		$fullTableName = $this->getTableInfo()->getTableFullName();
		if (empty ($sORDERBY)) 
			$sORDERBY="ROWNUM";
			
		$newSql = 	" SELECT $fields FROM " .
					"(					" .
					"		SELECT ROW_NUMBER() OVER ( ORDER BY {$sORDERBY} ) -1 as {$pradoNUMLIN} {$aliasedFields} " .
					"		FROM {$fullTableName} {$fieldsALIAS} $WhereInSubSelect" .
					") nn					" .
					" WHERE nn.{$pradoNUMLIN} >= {$offset} AND nn.{$pradoNUMLIN} < {$toReg} ";
		//echo $newSql."\n<br>\n";
		return $newSql;
	}

}
?>
