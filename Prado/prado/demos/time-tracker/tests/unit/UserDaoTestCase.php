<?php

require_once(dirname(__FILE__).'/BaseTestCase.php');

class UserDaoTestCase extends BaseTestCase
{
	protected $userDao;
	
	function setup()
	{
		parent::setup();
		$app = Prado::getApplication();
		$this->userDao = $app->getModule('daos')->getDao('UserDao');
		$this->flushDatabase();
	}
	
	function assertIsAdmin($user)
	{
		if(!$user)
			return $this->fail();	
		$this->assertEqual($user->getName(), 'admin');
		$this->assertEqual($user->getEmailAddress(), 'admin@pradosoft.com');
	}
	
	function assertSameUser($user1, $user2)
	{
		if(is_null($user1) || is_null($user2))
			return $this->fail();
			
		$this->assertEqual($user1->getName(), $user2->getName());
		$this->assertEqual($user1->getEmailAddress(), $user2->getEmailAddress());
	}
	
	function assertIsAdminRole($user)
	{
		if(is_null($user))
			return $this->fail();	
				
		$this->assertTrue($user->isInRole('admin'));
	}

	function assertIsManagerRole($user)
	{
		if(is_null($user))
			return $this->fail();	
				
		$this->assertTrue($user->isInRole('manager'));
	}

	function assertIsConsultantRole($user)
	{
		if(is_null($user))
			return $this->fail();	
				
		$this->assertTrue($user->isInRole('consultant'));
	}

	function assertNotConsultantRole($user)
	{
		if(is_null($user))
			return $this->fail();	
				
		$this->assertFalse($user->isInRole('consultant'));
	}
	
	function testGetUserByName()
	{	
		$user = $this->userDao->getUserByName('admin');
		$this->assertNotNull($user);	
		$this->assertIsAdmin($user);
	}

	function testGetNonExistentUser()
	{
		$user = $this->userDao->getUserByName('none');
		$this->assertNull($user);
	}
	
	function testGetUsers()
	{
		$users = $this->userDao->getAllUsers();
		$this->assertEqual(count($users), 3);
	}
	
	function testUserLogon()
	{
		$success = $this->userDao->validateUser('admin', 'admin');
		$this->assertTrue($success);
	}

	function testBadLogin()
	{
		$success = $this->userDao->validateUser('admin', 'hahah');
		$this->assertFalse($success);
	}
	
	
	function testAddNewUser()
	{
		$user = new TimeTrackerUser(new UserManager());
		$user->Name = "user1";
		$user->EmailAddress = 'user1@pradosoft.com';
		
		$this->userDao->addNewUser($user, 'password');
		
		$check = $this->userDao->getUserByName($user->Name);
		
		$this->assertSameUser($check, $user);
	}
	
	function testDeleteUserByName()
	{
		$this->userDao->deleteUserByName('admin');
		
		$admin = $this->userDao->getUserByName('admin');
		$this->assertNull($admin);
		
		$users = $this->userDao->getAllUsers();
		$this->assertEqual(count($users), 2);
	}
	
	function testAutoSignon()
	{
		$user = new TimeTrackerUser(new UserManager());
		$user->Name = "admin";
				
		$token = $this->userDao->createSignonToken($user);
		
		$check = $this->userDao->validateSignon($token);
		
		$this->assertIsAdmin($check);
	}
	

	function testBadAutoSignon()
	{
		$user = new TimeTrackerUser(new UserManager());
		$user->Name = "admin";
				
		$token = $this->userDao->createSignonToken($user);
		
		$check = $this->userDao->validateSignon('adasd');
		$this->assertNull($check);
	}

	function testAdminRoles()
	{
		$user = $this->userDao->getUserByName('admin');
		$this->assertIsAdminRole($user);	
		$this->assertIsManagerRole($user);
		$this->assertIsConsultantRole($user);
	}
	
	function testSetUserRoles()
	{
		$user = new TimeTrackerUser(new UserManager());
		$user->Name = "user1";
		$user->EmailAddress = 'user1@pradosoft.com';
		$user->Roles = array("manager", "consultant");

		$this->userDao->addNewUser($user, 'password');
		$check = $this->userDao->getUserByName('user1');
		
		$this->assertIsManagerRole($check);
		$this->assertIsConsultantRole($check);
	}
	
	function testSetUserRoleNoNullUser()
	{
		$user = new TimeTrackerUser(new UserManager());
		$user->Name = "user1";
		$user->EmailAddress = 'user1@pradosoft.com';
		$user->Roles = array("manager", "consultant");	
		
		try
		{
			$this->userDao->updateUserRoles($user);
			$this->fail();
		}
		catch(TDbException $e)
		{
			$this->pass();
		}
		
		$check = $this->sqlmap->queryForObject('GetUserByName', 'user1');
		$this->assertNull($check);
	}
	
	function testUpdateUser()
	{
		$user = $this->userDao->getUserByName('admin');
		$user->EmailAddress = 'something@pradosoft.com';
		$user->Roles = array('manager', 'admin');
		
		$this->userDao->updateUser($user);
		
		$check = $this->userDao->getUserByName('admin');
		$this->assertIsAdminRole($check);
		$this->assertIsManagerRole($check);
		$this->assertNotConsultantRole($check);
	}
	
	function testUpdateUserPassword()
	{
		$user = $this->userDao->getUserByName('admin');
		$user->EmailAddress = 'something@pradosoft.com';
		$user->Roles = array('manager', 'admin');
		
		$pass = 'newpasword';
		
		$this->userDao->updateUser($user, $pass);
		
		$success = $this->userDao->validateUser('admin', $pass);
		
		$this->assertTrue($success);
	}
	
	function testClearSignonTokens()
	{
		$user = new TimeTrackerUser(new UserManager());
		$user->Name = "admin";
				
		$token1 = $this->userDao->createSignonToken($user);
		sleep(1);
		$token2 = $this->userDao->createSignonToken($user);
		$this->assertNotEqual($token1, $token2);
		
		$check1 = $this->userDao->validateSignon($token1);
		$check2 = $this->userDao->validateSignon($token2);
		
		$this->assertIsAdmin($check1);
		$this->assertIsAdmin($check2);
		
		$this->userDao->clearSignonTokens($user);
		
		$check3 = $this->userDao->validateSignon($token1);
		$check4 = $this->userDao->validateSignon($token2);
		
		$this->assertNull($check3);
		$this->assertNull($check4);
	}
	
	function testClearAllSigonTokens()
	{
		$user1 = new TimeTrackerUser(new UserManager());
		$user1->Name = "admin";
		
		$user2 = new TimeTrackerUser(new UserManager());
		$user2->Name = "manager";
		
		$token1 = $this->userDao->createSignonToken($user1);
		$token2 = $this->userDao->createSignonToken($user2);
		
		$check1 = $this->userDao->validateSignon($token1);
		$check2 = $this->userDao->validateSignon($token2);
		
		$this->assertIsAdmin($check1);
		$this->assertNotNull($check2);
		$this->assertEqual($check2->Name, $user2->Name);
		
		$this->userDao->clearSignonTokens();
		
		$check3 = $this->userDao->validateSignon($token1);
		$check4 = $this->userDao->validateSignon($token2);
		
		$this->assertNull($check3);
		$this->assertNull($check4);
	}
}

?>