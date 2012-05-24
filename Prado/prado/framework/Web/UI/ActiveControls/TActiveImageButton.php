<?php
/**
 * TActiveImageButton class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveImageButton.php 2945 2011-06-01 20:48:19Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 */

/**
 * TActiveImageButton class.
 *
 * TActiveImageButton is the active control counter part to TImageButton.
 * When a TActiveImageButton is clicked, rather than a normal post back request a
 * callback request is initiated.
 *
 * The {@link onCallback OnCallback} event is raised during a callback request
 * and it is raise <b>after</b> the {@link onClick OnClick} event.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveImageButton.php 2945 2011-06-01 20:48:19Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveImageButton extends TImageButton implements IActiveControl, ICallbackEventHandler
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
	 * @return TCallbackClientSide client side request options.
	 */
	public function getClientSide()
	{
		return $this->getAdapter()->getBaseActiveControl()->getClientSide();
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

	/**
	 * Raises the callback event. This method is required by
	 * {@link ICallbackEventHandler ICallbackEventHandler} interface. If
	 * {@link getCausesValidation CausesValidation} is true, it will invoke the page's
	 * {@link TPage::validate} method first. It will raise
	 * {@link onClick OnClick} event first and then the {@link onCallback OnCallback} event.
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the event parameter
	 */
 	public function raiseCallbackEvent($param)
	{
		$this->raisePostBackEvent($param);
		$this->onCallback($param);
	}

	/**
	 * This method is invoked when a callback is requested. The method raises
	 * 'OnCallback' event to fire up the event handlers. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onCallback($param)
	{
		$this->raiseEvent('OnCallback', $this, $param);
	}

	/**
	 * Override parent implementation, no javascript is rendered here instead
	 * the javascript required for active control is registered in {@link addAttributesToRender}.
	 */
	protected function renderClientControlScript($writer)
	{
	}

	/**
	 * Register the x and y hidden input names of the position clicked.
	 * @param THtmlWriter the renderer.
	 */
	public function onPreRender($writer)
	{
		parent::onPreRender($writer);
		$uid = $uid=$this->getUniqueID();
		$this->getPage()->registerPostDataLoader($uid.'_x');
		$this->getPage()->registerPostDataLoader($uid.'_y');
	}

	/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control.
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		$writer->addAttribute('id',$this->getClientID());
		$this->getActiveControl()->registerCallbackClientScript(
			$this->getClientClassName(), $this->getPostBackOptions());
	}

	/**
	 * @return string corresponding javascript class name for this TActiveLinkButton.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TActiveImageButton';
	}
}

?>
