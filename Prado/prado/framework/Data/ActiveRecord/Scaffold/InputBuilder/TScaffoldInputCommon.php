<?php
/**
 * TScaffoldInputCommon class file.
 *
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @package System.Data.ActiveReecord.Scaffold.InputBuilder
 */
Prado::using('System.Data.ActiveRecord.Scaffold.InputBuilder.TScaffoldInputBase');

class TScaffoldInputCommon extends TScaffoldInputBase
{
	protected function setDefaultProperty($container, $control, $column, $record)
	{
		$control->setID(self::DEFAULT_ID);
		$control->setEnabled($this->getIsEnabled($column, $record));
		$container->Controls[] = $control;
	}

	protected function setNotNullProperty($container, $control, $column, $record)
	{
		$this->setDefaultProperty($container, $control, $column, $record);
		if(!$column->getAllowNull() && !$column->hasSequence())
			$this->createRequiredValidator($container, $column, $record);
	}

	protected function createBooleanControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$control = new TCheckBox();
		$control->setChecked(TPropertyValue::ensureBoolean($value));
		$control->setCssClass('boolean-checkbox');
		$this->setDefaultProperty($container, $control, $column, $record);
		return $control;
	}

	protected function createDefaultControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$control = new TTextBox();
		$control->setText($value);
		$control->setCssClass('default-textbox scaffold_input');
		if(($len=$column->getColumnSize())!==null)
			$control->setMaxLength($len);
		$this->setNotNullProperty($container, $control, $column, $record);
		return $control;
	}

	protected function getDefaultControlValue($container,$column, $record)
	{
		$control = $container->findControl(self::DEFAULT_ID);
		if($control instanceof TCheckBox)
			return $control->getChecked();
		else if($control instanceof TControl)
			return $control->getText();
	}

	protected function createMultiLineControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$control = new TTextBox();
		$control->setText($value);
		$control->setTextMode(TTextBoxMode::MultiLine);
		$control->setCssClass('multiline-textbox scaffold_input');
		$this->setNotNullProperty($container, $control, $column, $record);
		return $control;
	}

	protected function createYearControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$control = new TDropDownList();
		$years = array();
		$current = intval(@date('Y'));
		$from = $current-10; $to=$current+10;
		for($i = $from; $i <= $to; $i++)
			$years[$i] = $i;
		$control->setDataSource($years);
		$control->setSelectedValue(empty($value) ? $current : $value);
		$control->setCssClass('year-dropdown');
		$this->setNotNullProperty($container, $control, $column, $record);
		return $control;
	}

	protected function createIntegerControl($container, $column, $record)
	{
		$control = $this->createDefaultControl($container, $column, $record);
		$val = $this->createTypeValidator($container, $column, $record);
		$val->setDataType(TValidationDataType::Integer);
		$val->setErrorMessage('Please entery an integer.');
		return $control;
	}

	protected function createFloatControl($container, $column, $record)
	{
		$control = $this->createDefaultControl($container, $column, $record);
		$val = $this->createTypeValidator($container, $column, $record);
		$val->setDataType(TValidationDataType::Float);
		$val->setErrorMessage('Please entery a decimal number.');
		if(($max= $column->getMaxiumNumericConstraint())!==null)
		{
			$val = $this->createRangeValidator($container,$column,$record);
			$val->setDataType(TValidationDataType::Float);
			$val->setMaxValue($max);
			$val->setStrictComparison(true);
			$val->setErrorMessage('Please entery a decimal number strictly less than '.$max.'.');
		}
		return $control;
	}

	protected function createRequiredValidator($container, $column, $record)
	{
		$val = new TRequiredFieldValidator();
		$val->setErrorMessage('*');
		$val->setControlCssClass('required-input');
		$val->setCssClass('required');
		$val->setControlToValidate(self::DEFAULT_ID);
		$val->setValidationGroup($this->getParent()->getValidationGroup());
		$val->setDisplay(TValidatorDisplayStyle::Dynamic);
		$container->Controls[] = $val;
		return $val;
	}

	protected function createTypeValidator($container, $column, $record)
	{
		$val = new TDataTypeValidator();
		$val->setControlCssClass('required-input2');
		$val->setCssClass('required');
		$val->setControlToValidate(self::DEFAULT_ID);
		$val->setValidationGroup($this->getParent()->getValidationGroup());
		$val->setDisplay(TValidatorDisplayStyle::Dynamic);
		$container->Controls[] = $val;
		return $val;
	}

	protected function createRangeValidator($container, $column, $record)
	{
		$val = new TRangeValidator();
		$val->setControlCssClass('required-input3');
		$val->setCssClass('required');
		$val->setControlToValidate(self::DEFAULT_ID);
		$val->setValidationGroup($this->getParent()->getValidationGroup());
		$val->setDisplay(TValidatorDisplayStyle::Dynamic);
		$container->Controls[] = $val;
		return $val;
	}

	protected function createTimeControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$hours=array();
		for($i=0;$i<24;$i++) $hours[] = str_pad($i,2,'0',STR_PAD_LEFT);
		$mins=array();
		for($i=0;$i<60;$i++) $mins[] = str_pad($i,2,'0',STR_PAD_LEFT);
		$hour = intval(@date('H'));
		$min = intval(@date('i'));
		$sec = intval(@date('s'));
		if(!empty($value))
		{
			$match=array();
			if(preg_match('/(\d+):(\d+):?(\d+)?/', $value, $match))
			{
				$hour = $match[1];
				$min = $match[2];
				if(isset($match[3]))
					$sec=$match[3];
			}
		}

		$hcontrol = new TDropDownList();
		$hcontrol->setDataSource($hours);
		$hcontrol->setID(self::DEFAULT_ID);
		$hcontrol->dataBind();
		$hcontrol->setSelectedValue(intval($hour));
		$container->Controls[] = $hcontrol;
		$container->Controls[] = ' : ';

		$mcontrol = new TDropDownList();
		$mcontrol->setDataSource($mins);
		$mcontrol->dataBind();
		$mcontrol->setID('scaffold_time_min');
		$mcontrol->setSelectedValue(intval($min));
		$container->Controls[] = $mcontrol;
		$container->Controls[] = ' : ';

		$scontrol = new TDropDownList();
		$scontrol->setDataSource($mins);
		$scontrol->dataBind();
		$scontrol->setID('scaffold_time_sec');
		$scontrol->setSelectedValue(intval($sec));
		$container->Controls[] = $scontrol;

		return array($hcontrol,$mcontrol,$scontrol);
	}


	protected function createDateControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$control = new TDatePicker();
		$control->setFromYear(1900);
		$control->setInputMode(TDatePickerInputMode::DropDownList);
		$control->setDateFormat('yyyy-MM-dd');
		if(!empty($value))
			$control->setDate(substr($value,0,10));
		$control->setCssClass('date-dropdown');
		$this->setNotNullProperty($container, $control, $column, $record);
		return $control;
	}

	protected function createDateTimeControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$control = $this->createDateControl($container, $column, $record);
		$container->Controls[] = ' @ ';
		$time = $this->createTimeControl($container, $column, $record);
		if(!empty($value))
		{
			$match=array();
			if(preg_match('/(\d+):(\d+):?(\d+)?/', substr($value, 11), $match))
			{
				$time[0]->setSelectedValue(intval($match[1]));
				$time[1]->setSelectedValue(intval($match[2]));
				if(isset($match[3]))
					$time[2]->setSelectedValue(intval($match[3]));
			}
		}
		$time[0]->setID('scaffold_time_hour');
		return array($control, $time[0], $time[1], $time[2]);
	}

	protected function getDateTimeValue($container, $column, $record)
	{
		$date = $container->findControl(self::DEFAULT_ID)->getDate();
		$hour = $container->findControl('scaffold_time_hour')->getSelectedValue();
		$mins = $container->findControl('scaffold_time_min')->getSelectedValue();
		$secs = $container->findControl('scaffold_time_sec')->getSelectedValue();
		return "{$date} {$hour}:{$mins}:{$secs}";
	}

	protected function getTimeValue($container, $column, $record)
	{
		$hour = $container->findControl(self::DEFAULT_ID)->getSelectedValue();
		$mins = $container->findControl('scaffold_time_min')->getSelectedValue();
		$secs = $container->findControl('scaffold_time_sec')->getSelectedValue();
		return "{$hour}:{$mins}:{$secs}";
	}

	protected function createSetControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$selectedValues = preg_split('/\s*,\s*/', $value);
		$control = new TCheckBoxList();
		$values = $column->getDbTypeValues();
		$control->setDataSource($values);
		$control->dataBind();
		$control->setSelectedIndices($this->getMatchingIndices($values,$selectedValues));
		$control->setID(self::DEFAULT_ID);
		$control->setCssClass('set-checkboxes');
		$this->setNotNullProperty($container, $control, $column, $record);
		return $control;
	}

	protected function getMatchingIndices($checks, $values)
	{
		$index=array();
		for($i=0, $k=count($checks); $i<$k; $i++)
		{
			if(in_array($checks[$i], $values))
				$index[] = $i;
		}
		return $index;
	}

	protected function createEnumControl($container, $column, $record)
	{
		$value = $this->getRecordPropertyValue($column, $record);
		$selectedValues = preg_split('/\s*,\s*/', $value);
		$control = new TRadioButtonList();
		$values = $column->getDbTypeValues();
		$control->setDataSource($values);
		$control->dataBind();
		$index = $this->getMatchingIndices($values,$selectedValues);
		if(count($index) > 0)
			$control->setSelectedIndex($index[0]);
		$control->setID(self::DEFAULT_ID);
		$control->setCssClass('enum-radio-buttons');
		$this->setNotNullProperty($container, $control, $column, $record);
		return $control;
	}

	protected function getSetValue($container, $column, $record)
	{
		$value=array();
		foreach($container->findControl(self::DEFAULT_ID)->getItems() as $item)
		{
			if($item->getSelected())
				$value[] = $item->getText();
		}
		return implode(',', $value);
	}

	protected function getEnumValue($container, $column, $record)
	{
		return $container->findControl(self::DEFAULT_ID)->getSelectedItem()->getText();
	}
}

