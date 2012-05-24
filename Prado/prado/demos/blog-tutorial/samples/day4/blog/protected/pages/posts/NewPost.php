<?php

class NewPost extends TPage
{
	/**
	 * Creates a new post if all inputs are valid.
	 * This method responds to the OnClick event of the "create" button.
	 * @param mixed event sender
	 * @param mixed event parameter
	 */
	public function createButtonClicked($sender,$param)
	{
		if($this->IsValid)  // when all validations succeed
		{
			// populates a PostRecord object with user inputs
			$postRecord=new PostRecord;
			// using SafeText instead of Text avoids Cross Site Scripting attack
			$postRecord->title=$this->TitleEdit->SafeText;
			$postRecord->content=$this->ContentEdit->SafeText;
			$postRecord->author_id=$this->User->Name;
			$postRecord->create_time=time();
			$postRecord->status=0;

			// saves to the database via Active Record mechanism
			$postRecord->save();

			// redirects the browser to the newly created post page
			$url=$this->Service->constructUrl('posts.ReadPost',array('id'=>$postRecord->post_id));
			$this->Response->redirect($url);
		}
	}
}

?>