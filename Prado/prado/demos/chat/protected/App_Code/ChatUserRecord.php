<?php

class ChatUserRecord extends TActiveRecord
{
	const TABLE='chat_users';

	public $username;
	private $_last_activity;

	public function getLast_Activity()
	{
		if($this->_last_activity === null)
			$this->_last_activity = time();
		return $this->_last_activity;
	}

	public function setLast_Activity($value)
	{
		$this->_last_activity = $value;
	}

	public static function finder($className=__CLASS__)
	{
		return parent::finder($className);
	}

	public function getUserList()
	{
		$this->deleteAll('last_activity < ?', time()-300); //5 min inactivity
		$content = '<ul>';
		foreach($this->findAll() as $user)
		{
			$content .= '<li>'.htmlspecialchars($user->username).'</li>';
		}
		$content .= '</ul>';

		return $content;
	}
}

?>