<?php
/**
 * TCallbackClientScript class file
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TCallbackClientScript.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 */

/**
 * TCallbackClientScript class.
 *
 * The TCallbackClientScript class provides corresponding methods that can be
 * executed on the client-side (i.e. the browser client that is viewing
 * the page) during a callback response.
 *
 * The avaiable methods includes setting/clicking input elements, changing Css
 * styles, hiding/showing elements, and adding visual effects to elements on the
 * page. The client-side methods can be access through the CallbackClient
 * property available in TPage.
 *
 * For example, to hide "$myTextBox" element during callback response, do
 * <code>
 * $this->getPage()->getCallbackClient()->hide($myTextBox);
 * </code>
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @version $Id: TCallbackClientScript.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TCallbackClientScript extends TApplicationComponent
{
	/**
	 * @var TList list of client functions to execute.
	 */
	private $_actions;

	/**
	 * Constructor.
	 */
	public function __construct()
	{
		$this->_actions = new TList;
	}

	/**
	 * @return array list of client function to be executed during callback
	 * response.
	 */
	public function getClientFunctionsToExecute()
	{
		return $this->_actions->toArray();
	}

	/**
	 * Executes a client-side statement.
	 * @param string javascript function name
	 * @param array list of arguments for the function
	 */
	public function callClientFunction($function, $params=null)
	{
		if(!is_array($params))
			$params = array($params);

		if(count($params) > 0)
		{
			if($params[0] instanceof TControl)
				$params[0] = $params[0]->getClientID();
		}
		$this->_actions->add(array($function => $params));
	}

	/**
	 * Client script to set the value of a particular input element.
	 * @param TControl control element to set the new value
	 * @param string new value
	 */
	public function setValue($input, $text)
	{
		$this->callClientFunction('Prado.Element.setValue', array($input, $text));
	}

	/**
	 * Client script to select/clear/check a drop down list, check box list,
	 * or radio button list.
	 * The second parameter determines the selection method. Valid methods are
	 *  - <b>Value</b>, select or check by value
	 *  - <b>Values</b>, select or check by a list of values
	 *  - <b>Index</b>, select or check by index (zero based index)
	 *  - <b>Indices</b>, select or check by a list of index (zero based index)
	 *  - <b>Clear</b>, clears or selections or checks in the list
	 *  - <b>All</b>, select all
	 *  - <b>Invert</b>, invert the selection.
	 * @param TControl list control
	 * @param string selection method
	 * @param string|int the value or index to select/check.
	 * @param string selection control type, either 'check' or 'select'
	 */
	public function select($control, $method='Value', $value=null, $type=null)
	{
		$method = TPropertyValue::ensureEnum($method,
				'Value', 'Index', 'Clear', 'Indices', 'Values', 'All', 'Invert');
		$type = ($type===null) ? $this->getSelectionControlType($control) : $type;
		$total = $this->getSelectionControlIsListType($control) ? $control->getItemCount() : 1;
		$this->callClientFunction('Prado.Element.select',
				array($control, $type.$method, $value, $total));
	}

	private function getSelectionControlType($control)
	{
		if(is_string($control)) return 'check';
		if($control instanceof TCheckBoxList)
			return 'check';
		if($control instanceof TCheckBox)
			return 'check';
		return 'select';
	}

	private function getSelectionControlIsListType($control)
	{
		return $control instanceof TListControl;
	}

	/**
	 * Client script to click on an element. <b>This client-side function is unpredictable.</b>
	 * 
	 * @param TControl control element or element id
	 */
	public function click($control)
	{
		$this->callClientFunction('Prado.Element.click', $control);
	}

	/**
	 * Client script to check or uncheck a checkbox or radio button.
	 * @param TControl control element or element id
	 * @param boolean check or uncheck the checkbox or radio button.
	 */
	public function check($checkbox, $checked=true)
	{
		$this->select($checkbox, "Value", $checked);
	}

	/**
	 * Raise the client side event (given by $eventName) on a particular element.
	 * @param TControl control element or element id
	 * @param string Event name, e.g. "click"
	 */
	public function raiseClientEvent($control, $eventName)
	{
		$this->callClientFunction('Event.fireEvent',
				array($control, strtolower($eventName)));
	}

	/**
	 * Sets the attribute of a particular control.
	 * @param TControl control element or element id
	 * @param string attribute name
	 * @param string attribute value
	 */
	public function setAttribute($control, $name, $value)
	{
        // Attributes should be applied on Surrounding tag, except for 'disabled' attribute
		if ($control instanceof ISurroundable && strtolower($name)!=='disabled')
            $control=$control->getSurroundingTagID();
		$this->callClientFunction('Prado.Element.setAttribute',array($control, $name, $value));
	}

	/**
	 * Sets the options of a select input element.
	 * @param TControl control element or element id
	 * @param TCollection a list of new options
	 */
	public function setListItems($control, $items)
	{
		$options = array();
		if($control instanceof TListControl)
		{
			$promptText		= $control->getPromptText();
			$promptValue	= $control->getPromptValue();
			
			if($promptValue==='')
				$promptValue = $promptText;
	
			if($promptValue!=='')
				$options[] = array($promptText, $promptValue);
		}
		
		foreach($items as $item)
		{
			if($item->getHasAttributes())
				$options[] =  array($item->getText(),$item->getValue(), $item->getAttributes()->itemAt('Group'));
			else
				$options[] = array($item->getText(),$item->getValue());
		}
		$this->callClientFunction('Prado.Element.setOptions', array($control, $options));
	}

	/**
	 * Shows an element by changing its CSS display style as empty.
	 * @param TControl control element or element id
	 */
	public function show($element)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Element.show', $element);
	}

	/**
	 * Hides an element by changing its CSS display style to "none".
	 * @param TControl control element or element id
	 */
	public function hide($element)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Element.hide', $element);
	}

	/**
	 * Toggles the visibility of the element.
	 * @param TControl control element or element id
	 * @param string visual effect, such as, 'appear' or 'slide' or 'blind'.
	 * @param array additional options.
	 */
	public function toggle($element, $effect=null, $options=array())
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Element.toggle', array($element,$effect,$options));
	}

	/**
	 * Removes an element from the HTML page.
	 * @param TControl control element or element id
	 */
	public function remove($element)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Element.remove', $element);
	}

	public function addPostDataLoader($name)
	{
		$this->callClientFunction('Prado.CallbackRequest.addPostLoaders', $name);
	}

	/**
	 * Update the element's innerHTML with new content.
	 * @param TControl control element or element id
	 * @param TControl new HTML content, if content is of a TControl, the
	 * controls render method is called.
	 */
	public function update($element, $content)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->replace($element, $content, 'Element.update');
	}

	/**
	 * Add a Css class name to the element.
	 * @param TControl control element or element id
	 * @param string CssClass name to add.
	 */
	public function addCssClass($element, $cssClass)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Element.addClassName', array($element, $cssClass));
	}

	/**
	 * Remove a Css class name from the element.
	 * @param TControl control element or element id
	 * @param string CssClass name to remove.
	 */
	public function removeCssClass($element, $cssClass)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Element.removeClassName', array($element, $cssClass));
	}

	/**
	 * Sets the CssClass of an element.
	 * @param TControl control element or element id
	 * @param string new CssClass name for the element.
	 */
	/*public function setCssClass($element, $cssClass)
	{
		$this->callClientFunction('Prado.Element.CssClass.set', array($element, $cssClass));
	}*/

	/**
	 * Scroll the top of the browser viewing area to the location of the
	 * element.
	 * @param TControl control element or element id
	 */
	public function scrollTo($element)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Element.scrollTo', $element);
	}

	/**
	 * Focus on a particular element.
	 * @param TControl control element or element id.
	 */
	public function focus($element)
	{
		$this->callClientFunction('Prado.Element.focus', $element);
	}

	/**
	 * Sets the style of element. The style must be a key-value array where the
	 * key is the style property and the value is the style value.
	 * @param TControl control element or element id
	 * @param array list of key-value pairs as style property and style value.
	 */
	public function setStyle($element, $styles)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction('Prado.Element.setStyle', array($element, $styles));
	}

	/**
	 * Append a HTML fragement to the element.
	 * @param TControl control element or element id
	 * @param string HTML fragement or the control to be rendered
	 */
	public function appendContent($element, $content)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->replace($element, $content, 'Prado.Element.Insert.append');
	}

	/**
	 * Prepend a HTML fragement to the element.
	 * @param TControl control element or element id
	 * @param string HTML fragement or the control to be rendered
	 */
	public function prependContent($element, $content)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->replace($element, $content, 'Prado.Element.Insert.prepend');
	}

	/**
	 * Insert a HTML fragement after the element.
	 * @param TControl control element or element id
	 * @param string HTML fragement or the control to be rendered
	 */
	public function insertContentAfter($element, $content)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->replace($element, $content, 'Prado.Element.Insert.after');
	}

	/**
	 * Insert a HTML fragement in before the element.
	 * @param TControl control element or element id
	 * @param string HTML fragement or the control to be rendered
	 */
	public function insertContentBefore($element, $content)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->replace($element, $content, 'Prado.Element.Insert.before');
	}

	/**
	 * Replace the content of an element with new content. The new content can
	 * be a string or a TControl component. If the <tt>content</tt> parameter is
	 * a TControl component, its rendered method will be called and its contents
	 * will be used for replacement.
	 * @param TControl control element or HTML element id.
	 * @param string HTML fragement or the control to be rendered
	 * @param string replacement method, default is to replace the outter
	 * html content.
	 * @param string provide a custom boundary.
	 * @see insertAbout
	 * @see insertBelow
	 * @see insertBefore
	 * @see insertAfter
	 */
	protected function replace($element, $content, $method="Element.replace", $boundary=null)
	{
		if($content instanceof TControl)
		{
			$boundary = $this->getRenderedContentBoundary($content);
			$content = null;
		}
		else if($content instanceof THtmlWriter)
		{
			$boundary = $this->getResponseContentBoundary($content);
			$content = null;
		}

		$this->callClientFunction('Prado.Element.replace',
					array($element, $method, $content, $boundary));
	}

	/**
	 * Replace the content of an element with new content contained in writer.
	 * @param TControl control element or HTML element id.
	 * @param string HTML fragement or the control to be rendered
	 */
	public function replaceContent($element,$content)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->replace($element, $content);
	}

	/**
	 * Evaluate a block of javascript enclosed in a boundary.
	 * @param THtmlWriter writer for the content.
	 */
	public function evaluateScript($writer)
	{
		$this->replace(null, $writer, 'Prado.Element.evaluateScript');
	}

	/**
	 * Renders the control and return the content boundary from
	 * TCallbackResponseWriter. This method should only be used by framework
	 * component developers. The render() method is defered to be called in the
	 * TActivePageAdapter class.
	 * @param TControl control to be rendered on callback response.
	 * @return string the boundary for which the rendered content is wrapped.
	 */
	private function getRenderedContentBoundary($control)
	{
		$writer = $this->getResponse()->createHtmlWriter();
		$adapter = $control->getPage()->getAdapter();
		$adapter->registerControlToRender($control, $writer);
		return $writer->getWriter()->getBoundary();
	}

	/**
	 * @param THtmlWriter the writer responsible for rendering html content.
	 * @return string content boundary.
	 */
	private function getResponseContentBoundary($html)
	{
		if($html instanceof THtmlWriter)
		{
			if($html->getWriter() instanceof TCallbackResponseWriter)
				return $html->getWriter()->getBoundary();
		}
		return null;
	}

	/**
	 * Add a visual effect the element.
	 * @param string visual effect function name.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function visualEffect($type, $element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->callClientFunction($type, array($element, $options));
	}

	/**
	 * Visual Effect: Gradually make the element appear.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function appear($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Appear', $element, $options);
	}

	/**
	 * Visual Effect: Blind down.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function blindDown($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.BlindDown', $element, $options);
	}

	/**
	 * Visual Effect: Blind up.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function blindUp($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.BlindUp', $element, $options);

	}

	/**
	 * Visual Effect: Drop out.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function dropOut($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.DropOut', $element, $options);
	}

	/**
	 * Visual Effect: Gradually fade the element.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function fade($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Fade', $element, $options);
	}

	/**
	 * Visual Effect: Fold.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function fold($element, $options = null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Fold', $element, $options);
	}

	/**
	 * Visual Effect: Gradually make an element grow to a predetermined size.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function grow($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Grow', $element, $options);
	}

	/**
	 * Visual Effect: Gradually grow and fade the element.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function puff($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Puff', $element, $options);
	}

	/**
	 * Visual Effect: Pulsate.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function pulsate($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Pulsate', $element, $options);
	}

	/**
	 * Visual Effect: Shake the element.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function shake($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Shake', $element, $options);
	}

	/**
	 * Visual Effect: Shrink the element.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function shrink($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Shrink', $element, $options);
	}

	/**
	 * Visual Effect: Slide down.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function slideDown($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.SlideDown', $element, $options);
	}

	/**
	 * Visual Effect: Side up.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function slideUp($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.SlideUp', $element, $options);
	}

	/**
	 * Visual Effect: Squish the element.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function squish($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.Squish', $element, $options);
	}

	/**
	 * Visual Effect: Switch Off effect.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function switchOff($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Effect.SwitchOff', $element, $options);
	}

	/**
	 * Visual Effect: High light the element for about 2 seconds.
	 * @param TControl control element or element id
	 * @param array visual effect key-value pair options.
	 */
	public function highlight($element, $options=null)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$this->visualEffect('Prado.Effect.Highlight', $element, $options);
	}

	/**
	 * Set the opacity on a html element or control.
	 * @param TControl control element or element id
	 * @param float opacity value between 1 and 0
	 */
	public function setOpacity($element, $value)
	{
        if ($element instanceof ISurroundable) 
            $element=$element->getSurroundingTagID();
		$value = TPropertyValue::ensureFloat($value);
		$this->callClientFunction('Element.setOpacity', array($element,$value));
	}
}

