<?php
/**
 * TActiveImage class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveImage.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.ActiveControls
 */

/**
 * TActiveImage class.
 *
 * TActiveImage allows the {@link setAlternateText AlternateText},
 * {@link setImageAlign ImageAlign}, {@link setImageUrl ImageUrl},
 * and {@link setDescriptionUrl DescriptionUrl} to be updated during
 * a callback request.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveImage.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveImage extends TImage implements IActiveControl
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
	 * Sets the alternative text to be displayed in the TImage when the image is unavailable.
	 * @param string the alternative text
	 */
	public function setAlternateText($value)
	{
		parent::setAlternateText($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->setAttribute($this, 'alt', $value);
	}

	/**
	 * Sets the alignment of the image with respective to other elements on the page.
	 * Possible values include: absbottom, absmiddle, baseline, bottom, left,
	 * middle, right, texttop, and top. If an empty string is passed in,
	 * imagealign attribute will not be rendered.
	 * @param string the alignment of the image
	 */
	public function setImageAlign($value)
	{
		parent::setImageAlign($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->setAttribute($this, 'align', $value);
	}

	/**
	 * @param string the URL of the image file
	 */
	public function setImageUrl($value)
	{
		parent::setImageUrl($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->setAttribute($this, 'src', $value);
	}

	/**
	 * @param string the URL to the long description of the image.
	 */
	public function setDescriptionUrl($value)
	{
		parent::setDescriptionUrl($value);
		if($this->getActiveControl()->canUpdateClientSide())
			$this->getPage()->getCallbackClient()->setAttribute($this, 'longdesc', $value);
	}
}

?>
