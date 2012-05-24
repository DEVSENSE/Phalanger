<?php
/**
 * TIbmScaffoldInput class file.
 *
 * @author Cesar Ramos <cramos[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @package System.Data.ActiveReecord.Scaffold.InputBuilder
 */
Prado::using('System.Data.ActiveRecord.Scaffold.InputBuilder.TScaffoldInputCommon');

class TIbmScaffoldInput extends TScaffoldInputCommon
{
	protected function createControl($container, $column, $record)
	{
		switch(strtolower($column->getDbType()))
		{
			case 'date':
				return $this->createDateControl($container, $column, $record);
			case 'time':
				return $this->createTimeControl($container, $column, $record);
			case 'timestamp':
				return $this->createDateTimeControl($container, $column, $record);
			case 'smallint': case 'integer': case 'bigint':
				return $this->createIntegerControl($container, $column, $record);
			case 'decimal': case 'numeric': case 'real': case 'float': case 'double':
				return $this->createFloatControl($container, $column, $record);
			case 'char': case 'varchar':
				return $this->createMultiLineControl($container, $column, $record);
			default:
				return $this->createDefaultControl($container,$column, $record);
		}
	}

	protected function getControlValue($container, $column, $record)
	{
		switch(strtolower($column->getDbType()))
		{
			case 'date':
				return $container->findControl(self::DEFAULT_ID)->getDate();
			case 'time':
				return $this->getTimeValue($container, $column, $record);
			case 'timestamp':
				return $this->getDateTimeValue($container, $column, $record);
			default:
				return $this->getDefaultControlValue($container,$column, $record);
		}
	}
}

