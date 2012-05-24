<?php
/**
 * TActiveHyperLink class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveHyperLink.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 */

/**
 * TActiveHyperLink class.
 *
 * The active control counterpart of THyperLink component. When
 * {@link TBaseActiveControl::setEnableUpdate ActiveControl.EnableUpdate}
 * property is true the during a callback request, setting {@link setText Text}
 * property will also set the text of the label on the client upon callback
 * completion. Similarly, for other properties such as {@link setImageUrl ImageUrl},
 * {@link setNavigateUrl NavigateUrl} and {@link setTarget Target}.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveHyperLink.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveHyperLink extends THyperLink implements IActiveControl
{
	/**
	 * Creates a new callback control, sets the adapter to
	 * TActiveControlAdapter. If you override this class, be sure to set the
	 * adapter appropriately by, for example, by calling this constructor.
	 */
	public function __construct()
	{
		parent::__construct();
		$this->setAdapter(new TActiveControlAdapter($this));
	}

	/**
	 * @return TBaseActiveControl basic active control options.
	 */
	public function getActiveControl()
	{
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * On callback response, the inner HTMl of the label is updated.
	 * @param string the text value of the label
	 */
	public function setText($value)
	{
		parent::setText($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->update($this, $value);
	}

	/**
	 * Sets the location of image file of the THyperLink.
	 * @param string the image file location
	 */
	public function setImageUrl($value)
	{
		parent::setImageUrl($value);
		if($this->getActiveControl()->canUpdateClientSide() && $value !== '')
		{
			$textWriter = new TTextWriter;
			$renderer = Prado::createComponent($this->GetResponse()->getHtmlWriterType(), $textWriter);
			$this->createImage($value)->renderControl($renderer);
			$this->getPage()->getCallbackClient()->update($this, $textWriter->flush());
		}
	}

	/**
	 * Sets the URL to link to when the THyperLink component is clicked.
	 * @param string the URL
	 */
	public function setNavigateUrl($value)
	{
		parent::setNavigateUrl($value);
		if($this->getActiveControl()->canUpdateClientSide())
		{
			//replace &amp; with & and urldecode the url (setting the href using javascript is literal)
			$url = urldecode(str_replace('&amp;', '&', $value));
			$this->getPage()->getCallbackClient()->setAttribute($this, 'href', $url);
		}
	}

	/**
	 * Sets the target window or frame to display the Web page content linked to when the THyperLink component is clicked.
	 * @param string the target window, valid values include '_blank', '_parent', '_self', '_top' and empty string.
	 */
	public function setTarget($value)
	{
		parent::setTarget($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->setAttribute($this, 'target', $value);
	}
}

?>
