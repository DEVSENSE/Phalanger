<?php

Prado::using('Application.pages.ClassDefinition');

class Home extends TPage
{
	private $_classDefinition=null;

	public function getClassDefinition()
	{
		if(!$this->_classDefinition)
			$this->_classDefinition=new ClassDefinition;
		return $this->_classDefinition;
	}

	public function onInit($param)
	{
		parent::onInit($param);
		if(!$this->IsPostBack)
		{
			$properties=$this->ClassDefinition->Properties;
			$properties[]=new PropertyDefinition;
			$properties[]=new PropertyDefinition;
			$properties[]=new PropertyDefinition;
			$this->PropertyList->DataSource=$properties;
			$this->dataBind();
		}
	}

	public function propertyAction($sender,$param)
	{
		if($param->CommandName==='add')
			$this->ClassDefinition->Properties->add(new PropertyDefinition);
		if($param->CommandName==='remove')
			$this->ClassDefinition->Properties->removeAt($param->CommandParameter);
		else if($param->CommandName==='up')
		{
			$property=$this->ClassDefinition->Properties->itemAt($param->CommandParameter);
			$this->ClassDefinition->Properties->removeAt($param->CommandParameter);
			$this->ClassDefinition->Properties->insertAt($param->CommandParameter-1,$property);
		}
		else if($param->CommandName==='down')
		{
			$property=$this->ClassDefinition->Properties->itemAt($param->CommandParameter);
			$this->ClassDefinition->Properties->removeAt($param->CommandParameter);
			$this->ClassDefinition->Properties->insertAt($param->CommandParameter+1,$property);
		}
		$this->PropertyList->DataSource=$this->ClassDefinition->Properties;
		$this->PropertyList->dataBind();
	}

	public function eventAction($sender,$param)
	{
		if($param->CommandName==='add')
			$this->ClassDefinition->Events->add(new EventDefinition);
		else if($param->CommandName==='remove')
			$this->ClassDefinition->Events->removeAt($param->CommandParameter);
		else if($param->CommandName==='up')
		{
			$property=$this->ClassDefinition->Events->itemAt($param->CommandParameter);
			$this->ClassDefinition->Events->removeAt($param->CommandParameter);
			$this->ClassDefinition->Events->insertAt($param->CommandParameter-1,$property);
		}
		else if($param->CommandName==='down')
		{
			$property=$this->ClassDefinition->Events->itemAt($param->CommandParameter);
			$this->ClassDefinition->Events->removeAt($param->CommandParameter);
			$this->ClassDefinition->Events->insertAt($param->CommandParameter+1,$property);
		}
		$this->EventList->DataSource=$this->ClassDefinition->Events;
		$this->EventList->dataBind();
	}

	public function onLoad($param)
	{
		parent::onLoad($param);
		//if($this->IsPostBack && $this->IsValid)
		if($this->IsPostBack)
		{
			$def=$this->ClassDefinition;
			$def->reset();
			$def->ClassName=$this->ClassName->Text;
			$def->ParentClass=$this->ParentClass->Text;
			$def->Interfaces=$this->Interfaces->Text;
			$def->Comments=$this->Comments->Text;
			$def->Author=$this->AuthorName->Text;
			$def->Email=$this->AuthorEmail->Text;
			foreach($this->PropertyList->Items as $item)
			{
				$property=new PropertyDefinition;
				$property->Name=$item->PropertyName->Text;
				$property->Type=$item->PropertyType->Text;
				$property->DefaultValue=$item->DefaultValue->Text;
				$property->ReadOnly=$item->ReadOnly->Checked;
				$property->IsProtected=$item->IsProtected->Checked;
				$property->Comments=$item->Comments->Text;
				$property->Storage=$item->Storage->Text;
				$def->Properties[]=$property;
			}
			foreach($this->EventList->Items as $item)
			{
				$event=new EventDefinition;
				$event->Name=$item->EventName->Text;
				$event->Comments=$item->Comments->Text;
				$def->Events[]=$event;
			}
		}
	}

	public function generateCode($sender,$param)
	{
		$writer=Prado::createComponent('TTextWriter');
		$this->ClassDefinition->render($writer);
		$this->SourceCode->Text=$writer->flush();
	}
}

?>