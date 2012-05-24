<?php
/**
 * TWebControlAdapter class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TWebControlAdapter.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TWebControlAdapter class
 *
 * TWebControlAdapter is the base class for adapters that customize
 * rendering for the Web control to which the adapter is attached.
 * It may be used to modify the default markup or behavior for specific
 * browsers.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TWebControlAdapter.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TWebControlAdapter extends TControlAdapter
{
	/**
	 * Renders the control to which the adapter is attached.
	 * It calls {@link renderBeginTag}, {@link renderContents} and
	 * {@link renderEndTag} in order.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function render($writer)
	{
		$this->renderBeginTag($writer);
		$this->renderContents($writer);
		$this->renderEndTag($writer);
	}

	/**
	 * Renders the openning tag for the attached control.
	 * Default implementation calls the attached control's corresponding method.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function renderBeginTag($writer)
	{
		$this->getControl()->renderBeginTag($writer);
	}

	/**
	 * Renders the body contents within the attached control tag.
	 * Default implementation calls the attached control's corresponding method.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function renderContents($writer)
	{
		$this->getControl()->renderContents($writer);
	}

	/**
	 * Renders the closing tag for the attached control.
	 * Default implementation calls the attached control's corresponding method.
	 * @param THtmlWriter writer for the rendering purpose
	 */
	public function renderEndTag($writer)
	{
		$this->getControl()->renderEndTag($writer);
	}
}

