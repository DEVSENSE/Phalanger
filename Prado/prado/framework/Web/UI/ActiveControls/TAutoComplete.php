<?php
/**
 * TAutoComplete class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TAutoComplete.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active text box.
 */
Prado::using('System.Web.UI.ActiveControls.TActiveTextBox');
Prado::using('System.Web.UI.ActiveControls.TCallbackEventParameter');

/**
 * TAutoComplete class.
 *
 * TAutoComplete is a textbox that provides a list of suggestion on
 * the current partial word typed in the textbox. The suggestions are
 * requested using callbacks, and raises the {@link onSuggestion OnSuggestion}
 * event. The events of the TActiveText (from which TAutoComplete is extended from)
 * and {@link onSuggestion OnSuggestion} are mutually exculsive. That is,
 * if {@link onTextChange OnTextChange} and/or {@link onCallback OnCallback}
 * events are raise, then {@link onSuggestion OnSuggestion} will not be raise, and
 * vice versa.
 *
 * The list of suggestions should be set in the {@link onSuggestion OnSuggestion}
 * event handler. The partial word to match the suggestion is in the
 * {@link TCallbackEventParameter::getCallbackParameter TCallbackEventParameter::CallbackParameter}
 * property. The datasource of the TAutoComplete must be set using {@link setDataSource}
 * method. This sets the datasource for the suggestions repeater, available through
 * the {@link getSuggestions Suggestions} property. Header, footer templates and
 * other properties of the repeater can be access via the {@link getSuggestions Suggestions}
 * property and its sub-properties.
 *
 * The {@link setTextCssClass TextCssClass} property if set is used to find
 * the element within the Suggestions.ItemTemplate and Suggestions.AlternatingItemTemplate
 * that contains the actual text for the suggestion selected. That is,
 * only text inside elements with CSS class name equal to {@link setTextCssClass TextCssClass}
 * will be used as suggestions.
 *
 * To return the list of suggestions back to the browser, supply a non-empty data source
 * and call databind. For example,
 * <code>
 * function autocomplete_suggestion($sender, $param)
 * {
 *   $token = $param->getToken(); //the partial word to match
 *   $sender->setDataSource($this->getSuggestionsFor($token)); //set suggestions
 *   $sender->dataBind();
 * }
 * </code>
 *
 * The suggestion will be rendered when the {@link dataBind()} method is called
 * <strong>during a callback request</strong>.
 *
 * When an suggestion is selected, that is, when the use has clicked, pressed
 * the "Enter" key, or pressed the "Tab" key, the {@link onSuggestionSelected OnSuggestionSelected}
 * event is raised. The
 * {@link TCallbackEventParameter::getCallbackParameter TCallbackEventParameter::CallbackParameter}
 * property contains the index of the selected suggestion.
 *
 * TAutoComplete allows multiple suggestions within one textbox with each
 * word or phrase separated by any characters specified in the
 * {@link setSeparator Separator} property. The {@link setFrequency Frequency}
 * and {@link setMinChars MinChars} properties sets the delay and minimum number
 * of characters typed, respectively, before requesting for sugggestions.
 *
 * Use {@link onTextChange OnTextChange} and/or {@link onCallback OnCallback} events
 * to handle post backs due to {@link setAutoPostBack AutoPostBack}.
 *
 * In the {@link getSuggestions Suggestions} TRepater item template, all HTML text elements
 * are considered as text for the suggestion. Text within HTML elements with CSS class name
 * "informal" are ignored as text for suggestions.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TAutoComplete.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TAutoComplete extends TActiveTextBox implements INamingContainer
{
	/**
	 * @var ITemplate template for repeater items
	 */
	private $_repeater=null;
	/**
	 * @var TPanel result panel holding the suggestion items.
	 */
	private $_resultPanel=null;

	/**
	 * @return string word or token separators (delimiters).
	 */
	public function getSeparator()
	{
		return $this->getViewState('tokens', '');
	}

	/**
	 * @return string word or token separators (delimiters).
	 */
	public function setSeparator($value)
	{
		$this->setViewState('tokens', TPropertyValue::ensureString($value), '');
	}

	/**
	 * @return float maximum delay (in seconds) before requesting a suggestion.
	 */
	public function getFrequency()
	{
		return $this->getViewState('frequency', '');
	}

	/**
	 * @param float maximum delay (in seconds) before requesting a suggestion.
	 * Default is 0.4.
	 */
	public function setFrequency($value)
	{
		$this->setViewState('frequency', TPropertyValue::ensureFloat($value),'');
	}

	/**
	 * @return integer minimum number of characters before requesting a suggestion.
	 */
	public function getMinChars()
	{
		return $this->getViewState('minChars','');
	}

	/**
	 * @param integer minimum number of characters before requesting a suggestion.
	 */
	public function setMinChars($value)
	{
		$this->setViewState('minChars', TPropertyValue::ensureInteger($value), '');
	}

	/**
	 * @param string Css class name of the element to use for suggestion.
	 */
	public function setTextCssClass($value)
	{
		$this->setViewState('TextCssClass', $value);
	}

	/**
	 * @return string Css class name of the element to use for suggestion.
	 */
	public function getTextCssClass()
	{
		return $this->getViewState('TextCssClass');
	}

	/**
	 * Raises the callback event. This method is overrides the parent implementation.
	 * If {@link setAutoPostBack AutoPostBack} is enabled it will raise
	 * {@link onTextChanged OnTextChanged} event event and then the
	 * {@link onCallback OnCallback} event. The {@link onSuggest OnSuggest} event is
	 * raise if the request is to find sugggestions, the {@link onTextChanged OnTextChanged}
	 * and {@link onCallback OnCallback} events are <b>NOT</b> raised.
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the event parameter
	 */
 	public function raiseCallbackEvent($param)
	{
		$token = $param->getCallbackParameter();
		if(is_array($token) && count($token) == 2)
		{
			if($token[1] === '__TAutoComplete_onSuggest__')
			{
				$parameter = new TAutoCompleteEventParameter($this->getResponse(), $token[0]);
				$this->onSuggest($parameter);
			}
			else if($token[1] === '__TAutoComplete_onSuggestionSelected__')
			{
				$parameter = new TAutoCompleteEventParameter($this->getResponse(), null, $token[0]);
				$this->onSuggestionSelected($parameter);
			}
		}
		else if($this->getAutoPostBack())
			parent::raiseCallbackEvent($param);
	}

	/**
	 * This method is invoked when an autocomplete suggestion is requested.
	 * The method raises 'OnSuggest' event. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onSuggest($param)
	{
		$this->raiseEvent('OnSuggest', $this, $param);
	}

	/**
	 * This method is invoked when an autocomplete suggestion is selected.
	 * The method raises 'OnSuggestionSelected' event. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onSuggestionSelected($param)
	{
		$this->raiseEvent('OnSuggestionSelected', $this, $param);
	}

	/**
	 * @param array data source for suggestions.
	 */
	public function setDataSource($data)
	{
		$this->getSuggestions()->setDataSource($data);
	}

	/**
	 * Overrides parent implementation. Callback {@link renderSuggestions()} when
	 * page's IsCallback property is true.
	 */
	public function dataBind()
	{
		parent::dataBind();
		if($this->getPage()->getIsCallback())
			$this->renderSuggestions($this->getResponse()->createHtmlWriter());
	}

	/**
	 * @return TPanel suggestion results panel.
	 */
	public function getResultPanel()
	{
		if($this->_resultPanel===null)
			$this->_resultPanel = $this->createResultPanel();
		return $this->_resultPanel;
	}

	/**
	 * @return TPanel new instance of result panel. Default uses TPanel.
	 */
	protected function createResultPanel()
	{
		$panel = Prado::createComponent('System.Web.UI.WebControls.TPanel');
		$this->getControls()->add($panel);
		$panel->setID('result');
		return $panel;
	}

	/**
	 * @return TRepeater suggestion list repeater
	 */
	public function getSuggestions()
	{
		if($this->_repeater===null)
			$this->_repeater = $this->createRepeater();
		return $this->_repeater;
	}

	/**
	 * @return TRepeater new instance of TRepater to render the list of suggestions.
	 */
	protected function createRepeater()
	{
		$repeater = Prado::createComponent('System.Web.UI.WebControls.TRepeater');
		$repeater->setHeaderTemplate(new TAutoCompleteTemplate('<ul>'));
		$repeater->setFooterTemplate(new TAutoCompleteTemplate('</ul>'));
		$repeater->setItemTemplate(new TTemplate('<li><%# $this->DataItem %></li>',null));
		$this->getControls()->add($repeater);
		return $repeater;
	}

	/**
	 * Renders the end tag and registers javascript effects library.
	 */
	public function renderEndTag($writer)
	{
		$this->getPage()->getClientScript()->registerPradoScript('effects');
		parent::renderEndTag($writer);
		$this->renderResultPanel($writer);
	}

	/**
	 * Renders the result panel.
	 * @param THtmlWriter the renderer.
	 */
	protected function renderResultPanel($writer)
	{
		$this->getResultPanel()->render($writer);
	}

	/**
	 * Renders the suggestions during a callback respones.
	 * @param THtmlWriter the renderer.
	 */
	public function renderCallback($writer)
	{
		$this->renderSuggestions($writer);
	}

	/**
	 * Renders the suggestions repeater.
	 * @param THtmlWriter the renderer.
	 */
	public function renderSuggestions($writer)
	{
		if($this->getActiveControl()->canUpdateClientSide())
		{
			$this->getSuggestions()->render($writer);
			$boundary = $writer->getWriter()->getBoundary();
			$this->getResponse()->getAdapter()->setResponseData($boundary);
		}
	}

	/**
	 * @return array list of callback options.
	 */
	protected function getPostBackOptions()
	{
		//disallow page state update ?
		//$this->getActiveControl()->getClientSide()->setEnablePageStateUpdate(false);
		$options = array();
		if(strlen($string = $this->getSeparator()))
		{
			$string = strtr($string,array('\t'=>"\t",'\n'=>"\n",'\r'=>"\r"));
			$token = preg_split('//', $string, -1, PREG_SPLIT_NO_EMPTY);
			$options['tokens'] = TJavaScript::encode($token,false);
		}
		if($this->getAutoPostBack())
		{
			$options = array_merge($options,parent::getPostBackOptions());
			$options['AutoPostBack'] = true;
		}
		if(strlen($select = $this->getTextCssClass()))
			$options['select'] = $select;
		$options['ResultPanel'] = $this->getResultPanel()->getClientID();
		$options['ID'] = $this->getClientID();
		$options['EventTarget'] = $this->getUniqueID();
		if(($minchars=$this->getMinChars())!=='')
			$options['minChars'] = $minchars;
		if(($frequency=$this->getFrequency())!=='')
			$options['frequency'] = $frequency;
		$options['CausesValidation'] = $this->getCausesValidation();
		$options['ValidationGroup'] = $this->getValidationGroup();
		return $options;
	}

	/**
	 * Override parent implementation, no javascript is rendered here instead
	 * the javascript required for active control is registered in {@link addAttributesToRender}.
	 */
	protected function renderClientControlScript($writer)
	{
	}

	/**
	 * @return string corresponding javascript class name for this TActiveButton.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TAutoComplete';
	}
}

/**
 * TAutCompleteEventParameter contains the {@link getToken Token} requested by
 * the user for a partial match of the suggestions.
 *
 * The {@link getSelectedIndex SelectedIndex} is a zero-based index of the
 * suggestion selected by the user, -1 if not suggestion is selected.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TAutoComplete.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TAutoCompleteEventParameter extends TCallbackEventParameter
{
	private $_selectedIndex=-1;

	/**
	 * Creates a new TCallbackEventParameter.
	 */
	public function __construct($response, $parameter, $index=-1)
	{
		parent::__construct($response, $parameter);
		$this->_selectedIndex=$index;
	}

	/**
	 * @return int selected suggestion zero-based index, -1 if not selected.
	 */
	public function getSelectedIndex()
	{
		return $this->_selectedIndex;
	}

	/**
	 * @return string token for matching a list of suggestions.
	 */
	public function getToken()
	{
		return $this->getCallbackParameter();
	}
}

/**
 * TAutoCompleteTemplate class.
 *
 * TAutoCompleteTemplate is the default template for TAutoCompleteTemplate
 * item template.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TAutoComplete.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TAutoCompleteTemplate extends TComponent implements ITemplate
{
	private $_template;

	public function __construct($template)
	{
		$this->_template = $template;
	}
	/**
	 * Instantiates the template.
	 * It creates a {@link TDataList} control.
	 * @param TControl parent to hold the content within the template
	 */
	public function instantiateIn($parent)
	{
		$parent->getControls()->add($this->_template);
	}
}

