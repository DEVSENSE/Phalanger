<?php
/**
 * TDataRenderer class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TDataRenderer.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.1.2
 */

/**
 * TDataRenderer class
 *
 * TDataRenderer is the convenient base class for template-based renderer controls.
 * It extends {@link TTemplateControl} and implements the methods required
 * by the {@link IDataRenderer} interface.
 *
 * The following property is provided by TDataRenderer:
 * - {@link getData Data}: data associated with this renderer.

 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TDataRenderer.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.1.2
 */
abstract class TDataRenderer extends TTemplateControl implements IDataRenderer
{
	/**
	 * @var mixed data associated with this renderer
	 */
	private $_data;

	/**
	 * @return mixed data associated with the item
	 */
	public function getData()
	{
		return $this->_data;
	}

	/**
	 * @param mixed data to be associated with the item
	 */
	public function setData($value)
	{
		$this->_data=$value;
	}
}

