<?php
/**
 * CommentPortlet class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: CommentPortlet.php 1398 2006-09-08 19:31:03Z xue $
 */

Prado::using('Application.Portlets.Portlet');

/**
 * CommentPortlet class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class CommentPortlet extends Portlet
{
	public function onLoad($param)
	{
		parent::onLoad($param);
		$commentLimit=TPropertyValue::ensureInteger($this->Application->Parameters['RecentComments']);
		$comments=$this->Application->getModule('data')->queryComments('','ORDER BY create_time DESC',"LIMIT $commentLimit");
		foreach($comments as $comment)
		{
			$comment->ID=$this->Service->constructUrl('Posts.ViewPost',array('id'=>$comment->PostID)).'#c'.$comment->ID;
			if(strlen($comment->Content)>40)
				$comment->Content=substr($comment->Content,0,40).' ...';
		}
		$this->CommentList->DataSource=$comments;
		$this->CommentList->dataBind();
	}
}

?>