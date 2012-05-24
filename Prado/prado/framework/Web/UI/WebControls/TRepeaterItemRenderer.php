<?php
/**
 * TRepeaterItemRenderer class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TRepeaterItemRenderer.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.Web.UI.WebControls.TRepeater');
Prado::using('System.Web.UI.WebControls.TItemDataRenderer');

/**
 * TRepeaterItemRenderer class
 *
 * TRepeaterItemRenderer can be used as a convenient base class to
 * define an item renderer class specific for {@link TRepeater}.
 *
 * TRepeaterItemRenderer extends {@link TItemDataRenderer} and implements
 * the bubbling scheme for the OnCommand event of repeater items.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TRepeaterItemRenderer.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.1.0
 */
class TRepeaterItemRenderer extends TItemDataRenderer
{
	/**
	 * This method overrides parent's implementation by wrapping event parameter
	 * for <b>OnCommand</b> event with item information.
	 * @param TControl the sender of the event
	 * @param TEventParameter event parameter
	 * @return boolean whether the event bubbling should stop here.
	 */
	public function bubbleEvent($sender,$param)
	{
		if($param instanceof TCommandEventParameter)
		{
			$this->raiseBubbleEvent($this,new TRepeaterCommandEventParameter($this,$sender,$param));
			return true;
		}
		else
			return false;
	}
}

