<?php
/**
 * THtmlElement class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: THtmlElement.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.Web.UI.WebControls.TWebControl');

/**
 * THtmlElement class.
 *
 * THtmlElement represents a generic HTML element whose tag name is specified
 * via {@link setTagName TagName} property. Because THtmlElement extends from
 * {@link TWebControl}, it enjoys all its functionalities.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THtmlElement.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.WebControls
 * @since 3.1.2
 */
class THtmlElement extends TWebControl
{
	private $_tagName='span';

	/**
	 * @return string the tag name of this control. Defaults to 'span'.
	 */
	public function getTagName()
	{
		return $this->_tagName;
	}

	/**
	 * @param string the tag name of this control.
	 */
	public function setTagName($value)
	{
		$this->_tagName=$value;
	}
}
