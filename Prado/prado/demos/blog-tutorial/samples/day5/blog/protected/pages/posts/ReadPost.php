<?php

Prado::using('Application.BlogException');

class ReadPost extends TPage
{
	private $_post;
	/**
	 * Fetches the post data.
	 * This method is invoked by the framework when initializing the page
	 * @param mixed event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		// post id is passed via the 'id' GET parameter
		$postID=(int)$this->Request['id'];
		// retrieves PostRecord with author information filled in
		$this->_post=PostRecord::finder()->withAuthor()->findByPk($postID);
		if($this->_post===null)  // if post id is invalid
			throw new BlogException(500,'Unable to find the specified post.');
		// set the page title as the post title
		$this->Title=$this->_post->title;
	}

	/**
	 * @return PostRecord the PostRecord currently being viewed
	 */
	public function getPost()
	{
		return $this->_post;
	}

	/**
	 * Deletes the post currently being viewed
	 * This method is invoked when the user clicks on the "Delete" button
	 */
	public function deletePost($sender,$param)
	{
		// only the author or the administrator can delete a post
		if(!$this->canEdit())
			throw new THttpException('You are not allowed to perform this action.');
		// delete it from DB
		$this->_post->delete();
		// redirect the browser to the homepage
		$this->Response->redirect($this->Service->DefaultPageUrl);
	}

	/**
	 * @return boolean whether the current user can edit/delete the post being viewed
	 */
	public function canEdit()
	{
		// only the author or the administrator can edit/delete a post
		return $this->User->Name===$this->Post->author_id || $this->User->IsAdmin;
	}
}

?>