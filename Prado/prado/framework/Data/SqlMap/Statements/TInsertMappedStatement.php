<?php
/**
 * TInsertMappedStatement class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TInsertMappedStatement.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Statements
 */

/**
 * TInsertMappedStatement class.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TInsertMappedStatement.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap.Statements
 * @since 3.1
 */
class TInsertMappedStatement extends TMappedStatement
{
	public function executeQueryForMap($connection, $parameter,
								$keyProperty, $valueProperty=null)
	{
		throw new TSqlMapExecutionException(
				'sqlmap_cannot_execute_query_for_map', get_class($this), $this->getID());
	}

	public function executeUpdate($connection, $parameter)
	{
		throw new TSqlMapExecutionException(
				'sqlmap_cannot_execute_update', get_class($this), $this->getID());
	}

	public function executeQueryForList($connection, $parameter, $result=null,
										$skip=-1, $max=-1)
	{
		throw new TSqlMapExecutionException(
				'sqlmap_cannot_execute_query_for_list', get_class($this), $this->getID());
	}

	public function executeQueryForObject($connection, $parameter, $result=null)
	{
		throw new TSqlMapExecutionException(
				'sqlmap_cannot_execute_query_for_object', get_class($this), $this->getID());
	}
}

