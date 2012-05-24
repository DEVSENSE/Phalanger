<?php
/**
 * TTextWriter class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTextWriter.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.IO
 */

/**
 * TTextWriter class.
 *
 * TTextWriter implements a memory-based text writer.
 * Content written by TTextWriter are stored in memory
 * and can be obtained by calling {@link flush()}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTextWriter.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.IO
 * @since 3.0
 */
class TTextWriter extends TComponent implements ITextWriter
{
	private $_str='';

	/**
	 * Flushes the content that has been written.
	 * @return string the content being flushed
	 */
	public function flush()
	{
		$str=$this->_str;
		$this->_str='';
		return $str;
	}

	/**
	 * Writes a string.
	 * @param string string to be written
	 */
	public function write($str)
	{
		$this->_str.=$str;
	}

	/**
	 * Writers a string and terminates it with a newline.
	 * @param string content to be written
	 * @see write
	 */
	public function writeLine($str='')
	{
		$this->write($str."\n");
	}
}

