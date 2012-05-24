<?php
/**
 * BlogUser class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: BlogUser.php 1398 2006-09-08 19:31:03Z xue $
 */

Prado::using('System.Security.TUser');

/**
 * BlogUser class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class BlogUser extends TUser
{
	private $_id;

	public function getID()
	{
		return $this->_id;
	}

	public function setID($value)
	{
		$this->_id=$value;
	}

	public function getIsAdmin()
	{
		return $this->isInRole('admin');
	}

	public function saveToString()
	{
		$a=array($this->_id,parent::saveToString());
		return serialize($a);
	}

	public function loadFromString($data)
	{
		if(!empty($data))
		{
			list($id,$str)=unserialize($data);
			$this->_id=$id;
			return parent::loadFromString($str);
		}
		else
			return $this;
	}
}

?>