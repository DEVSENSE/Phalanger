<?php
/**
 * TLogger class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TLogger.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Util
 */

/**
 * TLogger class.
 *
 * TLogger records log messages in memory and implements the methods to
 * retrieve the messages with filter conditions, including log levels and
 * log categories.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLogger.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Util
 * @since 3.0
 */
class TLogger extends TComponent
{
	/**
	 * Log levels.
	 */
	const DEBUG=0x01;
	const INFO=0x02;
	const NOTICE=0x04;
	const WARNING=0x08;
	const ERROR=0x10;
	const ALERT=0x20;
	const FATAL=0x40;
	/**
	 * @var array log messages
	 */
	private $_logs=array();
	/**
	 * @var integer log levels (bits) to be filtered
	 */
	private $_levels;
	/**
	 * @var array list of categories to be filtered
	 */
	private $_categories;

	/**
	 * Logs a message.
	 * Messages logged by this method may be retrieved via {@link getLogs}.
	 * @param string message to be logged
	 * @param integer level of the message. Valid values include
	 * TLogger::DEBUG, TLogger::INFO, TLogger::NOTICE, TLogger::WARNING,
	 * TLogger::ERROR, TLogger::ALERT, TLogger::FATAL.
	 * @param string category of the message
	 */
	public function log($message,$level,$category='Uncategorized')
	{
		$this->_logs[]=array($message,$level,$category,microtime(true));
	}

	/**
	 * Retrieves log messages.
	 * Messages may be filtered by log levels and/or categories.
	 * A level filter is specified by an integer, whose bits indicate the levels interested.
	 * For example, (TLogger::INFO | TLogger::WARNING) specifies INFO and WARNING levels.
	 * A category filter is specified by concatenating interested category names
	 * with commas. A message whose category name starts with any filtering category
	 * will be returned. For example, a category filter 'System.Web, System.IO'
	 * will return messages under categories such as 'System.Web', 'System.IO',
	 * 'System.Web.UI', 'System.Web.UI.WebControls', etc.
	 * Level filter and category filter are combinational, i.e., only messages
	 * satisfying both filter conditions will they be returned.
	 * @param integer level filter
	 * @param string category filter
	 * @param array list of messages. Each array elements represents one message
	 * with the following structure:
	 * array(
	 *   [0] => message
	 *   [1] => level
	 *   [2] => category
	 *   [3] => timestamp (by microtime(), float number));
	 */
	public function getLogs($levels=null,$categories=null)
	{
		$this->_levels=$levels;
		$this->_categories=$categories;
		if(empty($levels) && empty($categories))
			return $this->_logs;
		else if(empty($levels))
			return array_values(array_filter(array_filter($this->_logs,array($this,'filterByCategories'))));
		else if(empty($categories))
			return array_values(array_filter(array_filter($this->_logs,array($this,'filterByLevels'))));
		else
		{
			$ret=array_values(array_filter(array_filter($this->_logs,array($this,'filterByLevels'))));
			return array_values(array_filter(array_filter($ret,array($this,'filterByCategories'))));
		}
	}

	/**
	 * Filter function used by {@link getLogs}
	 * @param array element to be filtered
	 */
	private function filterByCategories($value)
	{
		foreach($this->_categories as $category)
		{
			if($value[2]===$category || strpos($value[2],$category.'.')===0)
				return $value;
		}
		return false;
	}

	/**
	 * Filter function used by {@link getLogs}
	 * @param array element to be filtered
	 */
	private function filterByLevels($value)
	{
		if($value[1] & $this->_levels)
			return $value;
		else
			return false;
	}
}

