<?php
/**
 * CategoryPortlet class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: CategoryPortlet.php 1398 2006-09-08 19:31:03Z xue $
 */

Prado::using('Application.Portlets.Portlet');

/**
 * CategoryPortlet class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class CategoryPortlet extends Portlet
{
	public function onLoad($param)
	{
		parent::onLoad($param);
		$cats=$this->Application->getModule('data')->queryCategories();
		foreach($cats as $cat)
		{
			$cat->ID=$this->Service->constructUrl('Posts.ListPost',array('cat'=>$cat->ID));
			$cat->Name.=' (' . $cat->PostCount .')';
		}
		$this->CategoryList->DataSource=$cats;
		$this->CategoryList->dataBind();
	}
}

?>