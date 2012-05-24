<?php

Prado::using('Example.Person');

class crud1 extends TPage
{
    private function loadData()
    {
        $sqlmap = $this->Application->Modules['person-sample']->Client;
        $this->personList->DataSource = $sqlmap->queryForList('SelectAll');
		$this->personList->dataBind();
    }

	public function onLoad($param)
	{
		if(!$this->IsPostBack)
			$this->loadData();
	}
}

?>