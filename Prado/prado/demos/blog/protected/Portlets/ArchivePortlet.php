<?php
/**
 * ArchivePortlet class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: ArchivePortlet.php 1398 2006-09-08 19:31:03Z xue $
 */

Prado::using('Application.Portlets.Portlet');

/**
 * ArchivePortlet class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2006 PradoSoft
 * @license http://www.pradosoft.com/license/
 */
class ArchivePortlet extends Portlet
{
	private function makeMonthTime($timestamp)
	{
		$date=getdate($timestamp);
		return mktime(0,0,0,$date['mon'],1,$date['year']);
	}

	public function onLoad($param)
	{
		$currentTime=time();
		$startTime=$this->Application->getModule('data')->queryEarliestPostTime();
		if(empty($startTime))	// if no posts
			$startTime=$currentTime;

		// obtain the timestamp for the initial month
		$date=getdate($startTime);
		$startTime=mktime(0,0,0,$date['mon'],1,$date['year']);

		$date=getdate($currentTime);
		$month=$date['mon'];
		$year=$date['year'];

		$timestamps=array();
		while(true)
		{
			if(($timestamp=mktime(0,0,0,$month,1,$year))<$startTime)
				break;
			$timestamps[]=$timestamp;
			if(--$month===0)
			{
				$month=12;
				$year--;
			}
		}
		$this->MonthList->DataSource=$timestamps;
		$this->MonthList->dataBind();
	}
}

?>