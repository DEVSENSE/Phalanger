<?php
/**
 * TContentPlaceHolder class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TContentPlaceHolder.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TContentPlaceHolder class
 *
 * TContentPlaceHolder reserves a place on a template where a {@link TContent}
 * control can inject itself and its children in. TContentPlaceHolder and {@link TContent}
 * together implement a decoration pattern for prado templated controls.
 * A template control (called content control) can specify a master control
 * whose template contains some TContentPlaceHolder controls.
 * {@link TContent} controls on the content control's template will replace the corresponding
 * {@link TContentPlaceHolder} controls on the master control's template.
 * This is called content injection. It is done by matching the IDs of
 * {@link TContent} and {@link TContentPlaceHolder} controls.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TContentPlaceHolder.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TContentPlaceHolder extends TControl
{
	/**
	 * This method is invoked after the control is instantiated on a template.
	 * This overrides the parent implementation by registering the content placeholder
	 * control to the template owner control. The placeholder control will NOT
	 * be added to the potential parent control!
	 * @param TControl potential parent of this control
	 */
	public function createdOnTemplate($parent)
	{
		if(($id=$this->getID())==='')
			throw new TConfigurationException('contentplaceholder_id_required');
		$this->getTemplateControl()->registerContentPlaceHolder($id,$this);
		$parent->getControls()->add($this);
	}
}

