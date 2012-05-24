<?php

Prado::using('Example.Person');

class crud2 extends TPage
{
	private function sqlmap()
	{
		return $this->Application->Modules['person-sample']->Client;
	}

	private function loadData()
	{
		$this->personList->DataSource =  $this->sqlmap()->queryForList('SelectAll');
		$this->personList->dataBind();
	}

	public function onLoad($param)
	{
		if(!$this->IsPostBack)
			$this->loadData();
	}

	protected function editPerson($sender,$param)
	{
		$this->personList->EditItemIndex=$param->Item->ItemIndex;
		$this->loadData();
	}

	protected function deletePerson($sender, $param)
	{
		$id = $this->getKey($sender, $param);

		$this->sqlmap()->update("Delete", $id);
		$this->loadData();
	}

	protected function updatePerson($sender, $param)
	{
		$person = new Person();
		$person->FirstName = $this->getText($param, 0);
		$person->LastName = $this->getText($param, 1);
		$person->HeightInMeters = $this->getText($param, 2);
		$person->WeightInKilograms = $this->getText($param, 3);
		$person->ID = $this->getKey($sender, $param);

		$this->sqlmap()->update("Update", $person);
		$this->refreshList($sender, $param);
	}

	protected function addNewPerson($sender, $param)
	{
		$person = new Person;
		$person->FirstName = "-- New Person --";
		$this->sqlmap()->insert("Insert", $person);

		$this->loadData();;
	}

	protected function refreshList($sender, $param)
	{
		$this->personList->EditItemIndex=-1;
		$this->loadData();
	}

	private function getText($param, $index)
	{
		$item = $param->Item;
		return $item->Cells[$index]->Controls[0]->Text;
	}

	private function getKey($sender, $param)
	{
		return $sender->DataKeys[$param->Item->DataSourceIndex];
	}
}

?>