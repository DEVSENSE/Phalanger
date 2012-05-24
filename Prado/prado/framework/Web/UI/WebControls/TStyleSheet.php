<?php
/**
 * TStyleSheet class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TStyleSheet.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.WebControls
 */

/**
 * TStyleSheet class.
 *
 * TStyleSheet represents the link to a stylesheet file and/or a piece of
 * stylesheet code. To specify the link to a CSS file, set {@link setStyleSheetUrl StyleSheetUrl}.
 * The child rendering result of TStyleSheet is treated as CSS code and
 * is rendered within an appropriate style HTML element.
 * Therefore, if the child content is not empty, you should place the TStyleSheet
 * control in the head section of your page to conform to the HTML standard.
 * If only CSS file URL is specified, you may place the control anywhere on your page
 * and the style element will be rendered in the right position.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version : $  Tue Jul  4 04:38:16 EST 2006 $
 * @package System.Web.UI.WebControls
 * @since 3.0.2
 */
class TStyleSheet extends TControl
{
	/**
	 * @param string URL to the stylesheet file
	 */
	public function setStyleSheetUrl($value)
	{
		$this->setViewState('StyleSheetUrl', $value);
	}

	/**
	 * @return string URL to the stylesheet file
	 */
	public function getStyleSheetUrl()
	{
		return $this->getViewState('StyleSheetUrl', '');
	}

	/**
	 * @return string media type of the CSS (such as 'print', 'screen', etc.). Defaults to empty, meaning the CSS applies to all media types.
	 */
	public function getMediaType()
	{
		return $this->getViewState('MediaType','');
	}

	/**
	 * @param string media type of the CSS (such as 'print', 'screen', etc.). If empty, it means the CSS applies to all media types.
	 */
	public function setMediaType($value)
	{
		$this->setViewState('MediaType',$value,'');
	}

	/**
	 * Registers the stylesheet file and content to be rendered.
	 * This method overrides the parent implementation and is invoked right before rendering.
	 * @param mixed event parameter
	 */
	public function onPreRender($param)
	{
		if(($url=$this->getStyleSheetUrl())!=='')
			$this->getPage()->getClientScript()->registerStyleSheetFile($url,$url,$this->getMediaType());
	}

	/**
	 * Renders the control.
	 * This method overrides the parent implementation and renders nothing.
	 * @param ITextWriter writer
	 */
	public function render($writer)
	{
		if($this->getHasControls())
		{
			$writer->write("<style type=\"text/css\">\n/*<![CDATA[*/\n");
			$this->renderChildren($writer);
			$writer->write("\n/*]]>*/\n</style>\n");
		}
	}
}

?>
