<?php
/**
 * THyperLink class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.xisc.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.opensource.org/licenses/bsd-license.php BSD License
 * @version $Id: THyperLink.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * THyperLink class
 *
 * THyperLink displays a hyperlink on a page. The hyperlink URL is specified
 * via the {@link setNavigateUrl NavigateUrl} property, and link text is via
 * the {@link setText Text} property. It is also possible to display an image
 * by setting the {@link setImageUrl ImageUrl} property. In this case,
 * {@link getText Text} is displayed as the alternate text of the image.
 * The link target is specified via the {@link setTarget Target} property.
 * If both {@link getImageUrl ImageUrl} and {@link getText Text} are empty,
 * the content enclosed within the control tag will be rendered.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: THyperLink.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class THyperLink extends TWebControl implements IDataRenderer
{
	/**
	 * @return string tag name of the hyperlink
	 */
	protected function getTagName()
	{
		return 'a';
	}

	/**
	 * Adds attributes related to a hyperlink element to renderer.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		$isEnabled=$this->getEnabled(true);
		if($this->getEnabled() && !$isEnabled)
			$writer->addAttribute('disabled','disabled');
		parent::addAttributesToRender($writer);
		if(($url=$this->getNavigateUrl())!=='' && $isEnabled)
			$writer->addAttribute('href',$url);
		if(($target=$this->getTarget())!=='')
			$writer->addAttribute('target',$target);
	}

	/**
	 * Renders the body content of the hyperlink.
	 * @param THtmlWriter the writer for rendering
	 */
	public function renderContents($writer)
	{
		if(($imageUrl=$this->getImageUrl())==='')
		{
			if(($text=$this->getText())!=='')
				$writer->write(THttpUtility::htmlEncode($text));
			else if($this->getHasControls())
				parent::renderContents($writer);
			else
				$writer->write(THttpUtility::htmlEncode($this->getNavigateUrl()));
		}
		else
		{
			$this->createImage($imageUrl)->renderControl($writer);
		}
	}

	/**
	 * Gets the TImage for rendering the ImageUrl property. This is not for
	 * creating dynamic images.
	 * @param string image url.
	 * @return TImage image control for rendering.
	 */
	protected function createImage($imageUrl)
	{
		$image=Prado::createComponent('System.Web.UI.WebControls.TImage');
		$image->setImageUrl($imageUrl);
		if(($toolTip=$this->getToolTip())!=='')
			$image->setToolTip($toolTip);
		if(($text=$this->getText())!=='')
			$image->setAlternateText($text);
		$image->setBorderWidth('0');
		return $image;
	}

	/**
	 * @return string the text caption of the THyperLink
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * Sets the text caption of the THyperLink.
	 * @param string the text caption to be set
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value,'');
	}

	/**
	 * @return string the location of the image file for the THyperLink
	 */
	public function getImageUrl()
	{
		return $this->getViewState('ImageUrl','');
	}

	/**
	 * Sets the location of image file of the THyperLink.
	 * @param string the image file location
	 */
	public function setImageUrl($value)
	{
		$this->setViewState('ImageUrl',$value,'');
	}

	/**
	 * @return string the URL to link to when the THyperLink component is clicked.
	 */
	public function getNavigateUrl()
	{
		return $this->getViewState('NavigateUrl','');
	}

	/**
	 * Sets the URL to link to when the THyperLink component is clicked.
	 * @param string the URL
	 */
	public function setNavigateUrl($value)
	{
		$this->setViewState('NavigateUrl',$value,'');
	}

	/**
	 * Returns the URL to link to when the THyperLink component is clicked.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getText()}.
	 * @return string the text caption
	 * @see getText
	 * @since 3.1.0
	 */
	public function getData()
	{
		return $this->getText();
	}

	/**
	 * Sets the URL to link to when the THyperLink component is clicked.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setText()}.
	 * @param string the text caption to be set
	 * @see setText
	 * @since 3.1.0
	 */
	public function setData($value)
	{
		$this->setText($value);
	}

	/**
	 * @return string the target window or frame to display the Web page content linked to when the THyperLink component is clicked.
	 */
	public function getTarget()
	{
		return $this->getViewState('Target','');
	}

	/**
	 * Sets the target window or frame to display the Web page content linked to when the THyperLink component is clicked.
	 * @param string the target window, valid values include '_blank', '_parent', '_self', '_top' and empty string.
	 */
	public function setTarget($value)
	{
		$this->setViewState('Target',$value,'');
	}
}

