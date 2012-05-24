<?php
/**
 * TTextHighlighter class file
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTextHighlighter.php 2926 2011-05-25 09:34:54Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 */

Prado::using('System.3rdParty.TextHighlighter.Text.Highlighter',false);
Prado::using('System.3rdParty.TextHighlighter.Text.Highlighter.Renderer.Html',false);
Prado::using('System.Web.UI.WebControls.TTextProcessor');


/**
 * TTextHighlighter class.
 *
 * TTextHighlighter does syntax highlighting its body content, including
 * static text and rendering results of child controls.
 * You can set {@link setLanguage Language} to specify what kind of syntax
 * the body content is. Currently, TTextHighlighter supports the following
 * languages: ABAP, CPP, CSS, DIFF, DTD, HTML, JAVA, JAVASCRIPT, MYSQL, PERL,
 * PHP, PYTHON, RUBY, SQL, XML and PRADO, where PRADO refers to PRADO template
 * syntax. By setting {@link setShowLineNumbers ShowLineNumbers}
 * to true, the highlighted result may be shown with line numbers.
 *
 * Note, TTextHighlighter requires {@link THead} to be placed on the page template
 * because it needs to insert some CSS styles.
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @version $Id: TTextHighlighter.php 2926 2011-05-25 09:34:54Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTextHighlighter extends TTextProcessor
{
	private static $_lineNumberStyle=array(TTextHighlighterLineNumberStyle::Li => HL_NUMBERS_LI, TTextHighlighterLineNumberStyle::Table => HL_NUMBERS_TABLE);

	/**
	 * @return string tag name of the panel
	 */
	protected function getTagName()
	{
		return 'div';
	}

	/**
	 * @return string language whose syntax is to be used for highlighting. Defaults to 'php'.
	 */
	public function getLanguage()
	{
		return $this->getViewState('Language', 'php');
	}

	/**
	 * @param string language (case-insensitive) whose syntax is to be used for highlighting.
	 * Valid values are those file names (without suffix) that are contained
	 * in '3rdParty/TextHighlighter/Text/Highlighter'. Currently, the following languages are supported:
	 * ABAP, CPP, CSS, DIFF, DTD, HTML, JAVA, JAVASCRIPT,
	 * MYSQL, PERL, PHP, PRADO, PYTHON, RUBY, SQL, XML
	 * If a language is not supported, it will be displayed as plain text.
	 */
	public function setLanguage($value)
	{
		$this->setViewState('Language', $value, 'php');
	}

	/**
	 * @return boolean whether to show line numbers in the highlighted result.
	 */
	public function getShowLineNumbers()
	{
		return $this->getViewState('ShowLineNumbers', false);
	}

	/**
	 * @param boolean whether to show line numbers in the highlighted result.
	 */
	public function setShowLineNumbers($value)
	{
		$this->setViewState('ShowLineNumbers', TPropertyValue::ensureBoolean($value), false);
	}

	/**
	 * @return boolean true will show "Copy Code" link. Defaults to false.
	 */
	public function getEnableCopyCode()
	{
		return $this->getViewState('CopyCode', false);
	}

	/**
	 * @param boolean true to show the "Copy Code" link.
	 */
	public function setEnableCopyCode($value)
	{
		$this->setViewState('CopyCode', TPropertyValue::ensureBoolean($value), false);
	}

	/**
	 * @return TTextHighlighterLineNumberStyle style of row number, Table by default
	 */
	public function getLineNumberStyle()
	{
		return $this->getViewState('LineNumberStyle', TTextHighlighterLineNumberStyle::Table);
	}

	/**
	 * @param TTextHighlighterLineNumberStyle style of row number
	 */
	public function setLineNumberStyle($value)
	{
		$this->setViewState('LineNumberStyle', TPropertyValue::ensureEnum($value,'TTextHighlighterLineNumberStyle'));
	}

	/**
	 * @return integer tab size. Defaults to 4.
	 */
	public function getTabSize()
	{
		return $this->getViewState('TabSize', 4);
	}

	/**
	 * @param integer tab size
	 */
	public function setTabSize($value)
	{
		$this->setViewState('TabSize', TPropertyValue::ensureInteger($value));
	}

	/**
	 * Registers css style for the highlighted result.
	 * This method overrides parent implementation.
	 * @param THtmlWriter writer
	 */
	public function onPreRender($writer)
	{
		parent::onPreRender($writer);
		$this->registerStyleSheet();
		$this->getPage()->getClientScript()->registerPradoScript('prado');
	}

	/**
	 * Registers the stylesheet for presentation.
	 */
	protected function registerStyleSheet()
	{
		$cs=$this->getPage()->getClientScript();
		$cssFile=Prado::getPathOfNamespace('System.3rdParty.TextHighlighter.highlight','.css');
		$cssKey='prado:TTextHighlighter:'.$cssFile;
		if(!$cs->isStyleSheetFileRegistered($cssKey))
			$cs->registerStyleSheetFile($cssKey, $this->publishFilePath($cssFile));
	}

	/**
	 * Processes a text string.
	 * This method is required by the parent class.
	 * @param string text string to be processed
	 * @return string the processed text result
	 */
	public function processText($text)
	{
		try
		{
			$highlighter=Text_Highlighter::factory($this->getLanguage());
		}
		catch(Exception $e)
		{
			$highlighter=false;
		}
		if($highlighter===false)
			return ('<pre>'.htmlentities(trim($text)).'</pre>');

		$options["use_language"]=true;
		$options["tabsize"] = $this->getTabSize();
		if ($this->getShowLineNumbers())
			$options["numbers"] = self::$_lineNumberStyle[$this->getLineNumberStyle()];
		$highlighter->setRenderer(new Text_Highlighter_Renderer_Html($options));
		return $highlighter->highlight(trim($text));
	}

	/**
	 * @return string header template with "Copy code" link.
	 */
	protected function getHeaderTemplate()
	{
		$id = $this->getClientID();
		return TJavaScript::renderScriptBlock("new Prado.WebUI.TTextHighlighter('{$id}');");
	}
}

/**
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @version $Id: TTextHighlighter.php 2926 2011-05-25 09:34:54Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TTextHighlighterLineNumberStyle extends TEnumerable
{
	const Li='Li';
	const Table='Table';
}
