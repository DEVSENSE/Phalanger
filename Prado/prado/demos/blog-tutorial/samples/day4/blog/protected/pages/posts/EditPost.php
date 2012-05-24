<?php

class EditPost extends TPage
{
	/**
	 * Initializes the inputs with existing post data.
	 * This method is invoked by the framework when the page is being initialized.
	 * @param mixed event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		// Retrieves the existing user data. This is equivalent to:
		// $postRecord=$this->getPost();
		$postRecord=$this->Post;
		// Authorization check: only the author or the administrator can edit the post
		if($postRecord->author_id!==$this->User->Name && !$this->User->IsAdmin)
			throw new THttpException(500,'You are not allowed to edit this post.');

		if(!$this->IsPostBack)  // if the page is initially requested
		{
			// Populates the input controls with the existing post data
			$this->TitleEdit->Text=$postRecord->title;
			$this->ContentEdit->Text=$postRecord->content;
		}
	}

	/**
	 * Saves the post if all inputs are valid.
	 * This method responds to the OnClick event of the "Save" button.
	 * @param mixed event sender
	 * @param mixed event parameter
	 */
	public function saveButtonClicked($sender,$param)
	{
		if($this->IsValid)  // when all validations succeed
		{
			// Retrieves the existing user data. This is equivalent to:
			// $postRecord=$this->getPost();
			$postRecord=$this->Post;

			// Fetches the input data
			$postRecord->title=$this->TitleEdit->SafeText;
			$postRecord->content=$this->ContentEdit->SafeText;

			// saves to the database via Active Record mechanism
			$postRecord->save();

			// redirects the browser to the ReadPost page
			$url=$this->Service->constructUrl('posts.ReadPost',array('id'=>$postRecord->post_id));
			$this->Response->redirect($url);
		}
	}

	/**
	 * Returns the post data to be editted.
	 * @return PostRecord the post data to be editted.
	 * @throws THttpException if the post data is not found.
	 */
	protected function getPost()
	{
		// the ID of the post to be editted is passed via GET parameter 'id'
		$postID=(int)$this->Request['id'];
		// use Active Record to look for the specified post ID
		$postRecord=PostRecord::finder()->findByPk($postID);
		if($postRecord===null)
			throw new THttpException(500,'Post is not found.');
		return $postRecord;
	}
}

?>