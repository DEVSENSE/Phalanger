<?php
/**
 * ViewUser class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: ViewUser.php 1398 2006-09-08 19:31:03Z xue $
 */

/**
 * ViewUser class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class ViewUser extends BlogPage
{
	private $_userRecord=null;

	public function onInit($param)
	{
		parent::onInit($param);
		if(($id=$this->Request['id'])!==null)
			$id=TPropertyValue::ensureInteger($id);
		else
			$id=$this->User->ID;
		if(($this->_userRecord=$this->DataAccess->queryUserByID($id))===null)
			throw new BlogException(500,'profile_id_invalid',$id);
		$this->_userRecord->Email=strtr(strtoupper($this->_userRecord->Email),array('@'=>' at ','.'=>' dot '));
	}

	public function getProfile()
	{
		return $this->_userRecord;
	}
}

?>