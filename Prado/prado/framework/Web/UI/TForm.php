<?php
/**
 * TForm class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TForm.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 */

/**
 * TForm class
 *
 * TForm displays an HTML form. Besides regular body content,
 * it displays hidden fields, javascript blocks and files that are registered
 * through {@link TClientScriptManager}.
 *
 * A TForm is required for a page that needs postback.
 * Each page can contain at most one TForm. If multiple HTML forms are needed,
 * please use regular HTML form tags for those forms that post to different
 * URLs.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TForm.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 * @since 3.0
 */
class TForm extends TControl
{
	/**
	 * Registers the form with the page.
	 * @param mixed event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		$this->getPage()->setForm($this);
	}

	/**
	 * Adds form specific attributes to renderer.
	 * @param THtmlWriter writer
	 */
	protected function addAttributesToRender($writer)
	{
		$writer->addAttribute('id',$this->getClientID());
		$writer->addAttribute('method',$this->getMethod());
		$uri=$this->getRequest()->getRequestURI();
		$writer->addAttribute('action',str_replace('&','&amp;',str_replace('&amp;','&',$uri)));
		if(($enctype=$this->getEnctype())!=='')
			$writer->addAttribute('enctype',$enctype);

		$attributes=$this->getAttributes();
		$attributes->remove('action');
		$writer->addAttributes($attributes);

		if(($butt=$this->getDefaultButton())!=='')
		{
			if(($button=$this->findControl($butt))!==null)
				$this->getPage()->getClientScript()->registerDefaultButton($this, $button);
			else
				throw new TInvalidDataValueException('form_defaultbutton_invalid',$butt);
		}
	}

	/**
	 * Renders the form.
	 * @param THtmlWriter writer
	 */
	public function render($writer)
	{
		$page=$this->getPage();
		$page->beginFormRender($writer);
		$htmlWriter = Prado::createComponent($this->GetResponse()->getHtmlWriterType(), new TTextWriter());
		$this->renderChildren( $htmlWriter );
		$content = $htmlWriter->flush();
		$page->endFormRender($writer);

		$this->addAttributesToRender($writer);
		$writer->renderBeginTag('form');

		$cs=$page->getClientScript();
		if($page->getClientSupportsJavaScript())
		{
			$cs->renderHiddenFields($writer);
			$cs->renderScriptFiles($writer);
			$cs->renderBeginScripts($writer);

			$writer->write($content);

			$cs->renderEndScripts($writer);
		}
		else
		{
			$cs->renderHiddenFields($writer);
			$writer->write($content);
		}

		$writer->renderEndTag();
	}

	/**
	 * @return string id path to the default button control.
	 */
	public function getDefaultButton()
	{
		return $this->getViewState('DefaultButton','');
	}

	/**
	 * Sets a button to be default one in a form.
	 * A default button will be clicked if a user presses 'Enter' key within
	 * the form.
	 * @param string id path to the default button control.
	 */
	public function setDefaultButton($value)
	{
		$this->setViewState('DefaultButton',$value,'');
	}

	/**
	 * @return string form submission method. Defaults to 'post'.
	 */
	public function getMethod()
	{
		return $this->getViewState('Method','post');
	}

	/**
	 * @param string form submission method. Valid values include 'post' and 'get'.
	 */
	public function setMethod($value)
	{
		$this->setViewState('Method',TPropertyValue::ensureEnum($value,'post','get'),'post');
	}

	/**
	 * @return string the encoding type a browser uses to post data back to the server
	 */
	public function getEnctype()
	{
		return $this->getViewState('Enctype','');
	}

	/**
	 * @param string the encoding type a browser uses to post data back to the server.
	 * Commonly used types include
	 * - application/x-www-form-urlencoded : Form data is encoded as name/value pairs. This is the standard encoding format.
	 * - multipart/form-data : Form data is encoded as a message with a separate part for each control on the page.
	 * - text/plain : Form data is encoded in plain text, without any control or formatting characters.
	 */
	public function setEnctype($value)
	{
		$this->setViewState('Enctype',$value,'');
	}

	/**
	 * @return string form name, which is equal to {@link getUniqueID UniqueID}.
	 */
	public function getName()
	{
		return $this->getUniqueID();
	}
}

