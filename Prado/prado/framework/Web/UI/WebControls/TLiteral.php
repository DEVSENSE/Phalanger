<?php
/**
 * TLiteral class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TLiteral.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TLiteral class
 *
 * TLiteral displays a static text on the Web page.
 * TLiteral is similar to the TLabel control, except that the TLiteral
 * control does not have style properties (e.g. BackColor, Font, etc.)
 * You can programmatically control the text displayed in the control by setting
 * the {@link setText Text} property. The text displayed may be HTML-encoded
 * if the {@link setEncode Encode} property is set true (defaults to false).
 *
 * TLiteral will render the contents enclosed within its component tag
 * if {@link setText Text} is empty.
 *
 * Note, if {@link setEncode Encode} is false, make sure {@link setText Text}
 * does not contain unwanted characters that may bring security vulnerabilities.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLiteral.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TLiteral extends TControl implements IDataRenderer
{
	/**
	 * @return string the static text of the TLiteral
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * Sets the static text of the TLiteral
	 * @param string the text to be set
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value,'');
	}

	/**
	 * Returns the static text of the TLiteral.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getText()}.
	 * @return string the static text of the TLiteral
	 * @see getText
	 * @since 3.1.0
	 */
	public function getData()
	{
		return $this->getText();
	}

	/**
	 * Sets the static text of the TLiteral.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setText()}.
	 * @param string the static text of the TLiteral
	 * @see setText
	 * @since 3.1.0
	 */
	public function setData($value)
	{
		$this->setText($value);
	}

	/**
	 * @return boolean whether the rendered text should be HTML-encoded. Defaults to false.
	 */
	public function getEncode()
	{
		return $this->getViewState('Encode',false);
	}

	/**
	 * @param boolean  whether the rendered text should be HTML-encoded.
	 */
	public function setEncode($value)
	{
		$this->setViewState('Encode',TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * Renders the literal control.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function render($writer)
	{
		if(($text=$this->getText())!=='')
		{
			if($this->getEncode())
				$writer->write(THttpUtility::htmlEncode($text));
			else
				$writer->write($text);
		}
		else
			parent::render($writer);
	}
}

