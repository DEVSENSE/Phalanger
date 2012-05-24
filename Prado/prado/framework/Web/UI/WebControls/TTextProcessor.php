<?php
/**
 * TTextProcessor class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTextProcessor.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 */

/**
 * TTextProcessor class.
 *
 * TTextProcessor is the base class for classes that process or transform
 * text content into different forms. The text content to be processed
 * is specified by {@link setText Text} property. If it is not set, the body
 * content enclosed within the processor control will be processed and rendered.
 * The body content includes static text strings and the rendering result
 * of child controls.
 *
 * Note, all child classes must implement {@link processText} method.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TTextProcessor.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.Web.UI
 * @since 3.0.1
 */
abstract class TTextProcessor extends TWebControl
{
	/**
	 * Processes a text string.
	 * This method must be implemented by child classes.
	 * @param string text string to be processed
	 * @return string the processed text result
	 */
	abstract public function processText($text);

	/**
	 * HTML-decodes static text.
	 * This method overrides parent implementation.
	 * @param mixed object to be added as body content
	 */
	public function addParsedObject($object)
	{
		if(is_string($object))
			$object=html_entity_decode($object,ENT_QUOTES,'UTF-8');
		parent::addParsedObject($object);
	}

	/**
	 * @return string text to be processed
	 */
	public function getText()
	{
		return $this->getViewState('Text','');
	}

	/**
	 * @param string text to be processed
	 */
	public function setText($value)
	{
		$this->setViewState('Text',$value);
	}

	/**
	 * Renders body content.
	 * This method overrides the parent implementation by replacing
	 * the body content with the processed text content.
	 * @param THtmlWriter writer
	 */
	public function renderContents($writer)
	{
		if(($text=$this->getText())==='' && $this->getHasControls())
		{
			$htmlWriter = Prado::createComponent($this->GetResponse()->getHtmlWriterType(), new TTextWriter());
			parent::renderContents($htmlWriter);
			$text=$htmlWriter->flush();
		}
		if($text!=='')
			$writer->write($this->processText($text));
	}

}
