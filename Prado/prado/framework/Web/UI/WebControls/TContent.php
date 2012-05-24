<?php
/**
 * TContent class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TContent.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TContent class
 *
 * TContent specifies a block of content on a control's template
 * that will be injected at somewhere of the master control's template.
 * TContentPlaceHolder and {@link TContent} together implement a decoration
 * pattern for prado templated controls. A template control
 * (called content control) can specify a master control
 * whose template contains some TContentPlaceHolder controls.
 * {@link TContent} controls on the content control's template will replace the corresponding
 * {@link TContentPlaceHolder} controls on the master control's template.
 * This is called content injection. It is done by matching the IDs of
 * {@link TContent} and {@link TContentPlaceHolder} controls.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TContent.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TContent extends TControl implements INamingContainer
{
	/**
	 * This method is invoked after the control is instantiated on a template.
	 * This overrides the parent implementation by registering the content control
	 * to the template owner control.
	 * @param TControl potential parent of this control
	 */
	public function createdOnTemplate($parent)
	{
		if(($id=$this->getID())==='')
			throw new TConfigurationException('content_id_required');
		$this->getTemplateControl()->registerContent($id,$this);
	}
}

