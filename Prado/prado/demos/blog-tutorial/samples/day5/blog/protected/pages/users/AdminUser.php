<?php

class AdminUser extends TPage
{
	/**
	 * Populates the datagrid with user lists.
	 * This method is invoked by the framework when initializing the page
	 * @param mixed event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		// fetches all data account information
		$this->UserGrid->DataSource=UserRecord::finder()->findAll();
		// binds the data to interface components
		$this->UserGrid->dataBind();
	}

	/**
	 * Deletes a specified user record.
	 * This method responds to the datagrid's OnDeleteCommand event.
	 * @param TDataGrid the event sender
	 * @param TDataGridCommandEventParameter the event parameter
	 */
	public function deleteButtonClicked($sender,$param)
	{
		// obtains the datagrid item that contains the clicked delete button
		$item=$param->Item;
		// obtains the primary key corresponding to the datagrid item
		$username=$this->UserGrid->DataKeys[$item->ItemIndex];
		// deletes the user record with the specified username primary key
		UserRecord::finder()->deleteByPk($username);
	}
}

?>