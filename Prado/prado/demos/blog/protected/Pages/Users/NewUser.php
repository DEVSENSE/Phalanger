<?php
/**
 * NewUser class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: NewUser.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * NewUser class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class NewUser extends BlogPage
{
	public function onInit($param)
	{
		if(!$this->User->IsAdmin && !TPropertyValue::ensureBoolean($this->Application->Parameters['MultipleUser']))
			throw new BlogException(500,'newuser_registration_disallowed');
	}

	public function checkUsername($sender,$param)
	{
		$username=strtolower($this->Username->Text);
		$param->IsValid=$this->DataAccess->queryUserByName($username)===null;
	}

	public function createUser($sender,$param)
	{
		if($this->IsValid)
		{
			$userRecord=new UserRecord;
			$userRecord->Name=strtolower($this->Username->Text);
			$userRecord->FullName=$this->FullName->Text;
			$userRecord->Role=0;
			$userRecord->Password=md5($this->Password->Text);
			$userRecord->Email=$this->Email->Text;
			$userRecord->CreateTime=time();
			$userRecord->Website=$this->Website->Text;
			if(TPropertyValue::ensureBoolean($this->Application->Parameters['AccountApproval']))
				$userRecord->Status=UserRecord::STATUS_PENDING;
			else
				$userRecord->Status=UserRecord::STATUS_NORMAL;
			$this->DataAccess->insertUser($userRecord);
			$authManager=$this->Application->getModule('auth');
			$authManager->login($this->Username->Text,$this->Password->Text);
			$this->gotoDefaultPage();
		}
	}
}

?>