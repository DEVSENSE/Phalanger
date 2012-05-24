<?php
/**
 * TLabel class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TLabel.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TLabel class
 *
 * TLabel displays a piece of text on a Web page.
 * Use {@link setText Text} property to set the text to be displayed.
 * TLabel will render the contents enclosed within its component tag
 * if {@link setText Text} is empty.
 * To use TLabel as a form label, associate it with a control by setting the
 * {@link setForControl ForControl} property.
 * The associated control must be locatable within the label's naming container.
 * If the associated control is not visible, the label will not be rendered, either.
 *
 * Note, {@link setText Text} will NOT be encoded for rendering.
 * Make sure it does not contain dangerous characters that you want to avoid.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLabel.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TLabel extends TWebControl implements IDataRenderer
{
	private $_forControl='';

	/**
	 * @return string tag name of the label, returns 'label' if there is an associated control, 'span' otherwise.
	 */
	protected function getTagName()
	{
		return ($this->getForControl()==='')?'span':'label';
	}

	/**
	 * Adds attributes to renderer.
	 * @param THtmlWriter the renderer
	 * @throws TInvalidDataValueException if associated control cannot be found using the ID
	 */
	protected function addAttributesToRender($writer)
	{
		if($this->_forControl!=='')
			$writer->addAttribute('for',$this->_forControl);
		parent::addAttributesToRender($writer);
	}

	/**
	 * Renders the label.
	 * It overrides the parent implementation by checking if an associated
	 * control is visible or not. If not, the label will not be rendered.
	 * @param THtmlWriter writer
	 */
	public function render($writer)
	{
		if(($aid=$this->getForControl())!=='')
		{
			if($control=$this->findControl($aid))
			{
				if($control->getVisible(true))
				{
					$this->_forControl=$control->getClientID();
					parent::render($writer);
				}
			}
			else
				throw new TInvalidDataValueException('label_associatedcontrol_invalid',$aid);
		}
		else
			parent::render($writer);
	}

	/**
	 * Renders the body content of the label.
	 * @param THtmlWriter the renderer
	 */
	public function renderContents($writer)
	{
		if(($text=$this->getText())==='')
			parent::renderContents($writer);
		else
			$writer->write($text);
	}

	/**
	 * @return string the text value of the label
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * @param string the text value of the label
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value,'');
	}

	/**
	 * Returns the text value of the label.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getText()}.
	 * @return string the text value of the label
	 * @see getText
	 * @since 3.1.0
	 */
	public function getData()
	{
		return $this->getText();
	}

	/**
	 * Sets the text value of the label.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setText()}.
	 * @param string the text value of the label
	 * @see setText
	 * @since 3.1.0
	 */
	public function setData($value)
	{
		$this->setText($value);
	}

	/**
	 * @return string the associated control ID
	 */
	public function getForControl()
	{
		return $this->getViewState('ForControl','');
	}

	/**
	 * Sets the ID of the control that the label is associated with.
	 * The control must be locatable via {@link TControl::findControl} using the ID.
	 * @param string the associated control ID
	 */
	public function setForControl($value)
	{
		$this->setViewState('ForControl',$value,'');
	}
}

