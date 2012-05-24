<?php

class Contact extends TPage
{
	/**
	 * Event handler for the OnClick event of the submit button.
	 * @param TButton the button triggering the event
	 * @param TEventParameter event parameter (null here)
	 */
	public function submitButtonClicked($sender, $param)
	{
		if ($this->IsValid)  // check if input validation is successful
		{
			// obtain the user name, email, feedback from the textboxes
			$name = $this->Name->Text;
			$email = $this->Email->Text;
			$feedback = $this->Feedback->Text;

			// send an email to administrator with the above information
			$this->mailFeedback($name, $email, $feedback);
		}
	}

	protected function mailFeedback($name, $email, $feedback)
	{
		// implementation of sending the feedback email
	}
}

?>