<?php
/**
 * SearchPortlet class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: SearchPortlet.php 1509 2006-11-25 20:51:43Z xue $
 */

Prado::using('Application.Portlets.Portlet');

/**
 * SearchPortlet class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class SearchPortlet extends Portlet
{
	public function onInit($param)
	{
		parent::onInit($param);
		if(!$this->Page->IsPostBack && ($keyword=$this->Request['keyword'])!==null)
			$this->Keyword->Text=$keyword;
	}

	public function search($sender,$param)
	{
		$keyword=$this->Keyword->Text;
		$url=$this->Service->constructUrl('SearchPost',array('keyword'=>$keyword),false);
		$this->Response->redirect($url);
	}
}

?>