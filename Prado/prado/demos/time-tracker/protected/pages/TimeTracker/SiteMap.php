<?php
/**
 * SiteMap template class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: SiteMap.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 */

/**
 * SiteMap menu is rendered depending on user roles.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: SiteMap.php 1400 2006-09-09 03:13:44Z wei $
 * @package Demos
 * @since 3.1
 */
class SiteMap extends TTemplateControl
{
	/**
	 * Sets the active menu item using css class.
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		
		$page = explode('.',$this->Request->ServiceParameter);
		$active = null;
		switch($page[count($page)-1])
		{
			case 'ProjectList':
			case 'ProjectDetails':
				$active = $this->ProjectMenu;
				break;
			case 'UserList':
			case 'UserCreate':
				$active = $this->AdminMenu;
				break;
			case 'ReportProject':
			case 'ReportResource':
				$active = $this->ReportMenu;
				break;
			default:
				$active = $this->LogMenu;
				break;
		}
		
		//add 'active' string to place holder body.
		if(!is_null($active))
			$active->Controls[] = 'active';
	}
}

?>