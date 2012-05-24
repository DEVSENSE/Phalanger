<?php
/**
 * TSqliteScaffoldInput class file.
 *
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @package System.Data.ActiveReecord.Scaffold.InputBuilder
 */
Prado::using('System.Data.ActiveRecord.Scaffold.InputBuilder.TScaffoldInputCommon');

class TSqliteScaffoldInput extends TScaffoldInputCommon
{
	protected function createControl($container, $column, $record)
	{
		switch(strtolower($column->getDbType()))
		{
			case 'boolean':
				return $this->createBooleanControl($container, $column, $record);
			case 'date':
				return $this->createDateControl($container, $column, $record);
			case 'blob': case 'tinyblob': case 'mediumblob': case 'longblob':
			case 'text': case 'tinytext': case 'mediumtext': case 'longtext':
				return $this->createMultiLineControl($container, $column, $record);
			case 'year':
				return $this->createYearControl($container, $column, $record);
			case 'int': case 'integer': case 'tinyint': case 'smallint': case 'mediumint': case 'bigint':
				return $this->createIntegerControl($container, $column, $record);
			case 'decimal': case 'double': case 'float':
				return $this->createFloatControl($container, $column, $record);
			case 'time' :
				return $this->createTimeControl($container, $column, $record);
			case 'datetime': case 'timestamp':
				return $this->createDateTimeControl($container, $column, $record);
			default:
				return $this->createDefaultControl($container,$column, $record);
		}
	}

	protected function getControlValue($container, $column, $record)
	{
		switch(strtolower($column->getDbType()))
		{
			case 'boolean':
				return $container->findControl(self::DEFAULT_ID)->getChecked();
			case 'date':
				return $container->findControl(self::DEFAULT_ID)->getDate();
			case 'year':
				return $container->findControl(self::DEFAULT_ID)->getSelectedValue();
			case 'time':
				return $this->getTimeValue($container, $column, $record);
			case 'datetime': case 'timestamp':
				return $this->getDateTimeValue($container,$column, $record);
			default:
				return $this->getDefaultControlValue($container,$column, $record);
		}
	}

	protected function createDateControl($container, $column, $record)
	{
		$control = parent::createDateControl($container, $column, $record);
		$value = $this->getRecordPropertyValue($column, $record);
		if(!empty($value) && preg_match('/timestamp/i', $column->getDbType()))
			$control->setTimestamp(intval($value));
		return $control;
	}

	protected function createDateTimeControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$time = parent::createDateTimeControl($container, $column, $record);
		if(!empty($value) && preg_match('/timestamp/i', $column->getDbType()))
		{
			$s = Prado::createComponent('System.Util.TDateTimeStamp');
			$date = $s->getDate(intval($value));
			$time[1]->setSelectedValue($date['hours']);
			$time[2]->setSelectedValue($date['minutes']);
			$time[3]->setSelectedValue($date['seconds']);
		}
		return $time;
	}

	protected function getDateTimeValue($container, $column, $record)
	{
		if(preg_match('/timestamp/i', $column->getDbType()))
		{
			$time = $container->findControl(self::DEFAULT_ID)->getTimestamp();
			$s = Prado::createComponent('System.Util.TDateTimeStamp');
			$date = $s->getDate($time);
			$hour = $container->findControl('scaffold_time_hour')->getSelectedValue();
			$mins = $container->findControl('scaffold_time_min')->getSelectedValue();
			$secs = $container->findControl('scaffold_time_sec')->getSelectedValue();
			return $s->getTimeStamp($hour,$mins,$secs,$date['mon'],$date['mday'],$date['year']);
		}
		else
			return parent::getDateTimeValue($container, $column, $record);
	}
}

