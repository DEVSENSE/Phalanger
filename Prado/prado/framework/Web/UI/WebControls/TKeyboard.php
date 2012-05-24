<?php
/**
 * TKeyboard class file.
 *
 * @author Sergey Morkovkin <sergeymorkovkin@mail.ru> and Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TKeyboard.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */

/**
 * Class TKeyboard.
 *
 * TKeyboard displays a virtual keyboard that users can click on to enter input in
 * an associated text box. It helps to reduce the keyboard recording hacking.
 *
 * To use TKeyboard, write a template like following:
 * <code>
 * <com:TTextBox ID="PasswordInput" />
 * <com:TKeyboard ForControl="PasswordInput" />
 * </code>
 *
 * A TKeyboard control is associated with a {@link TTextBox} control by specifying {@link setForControl ForControl}
 * to be the ID of that control. When the textbox is in focus, a virtual keyboard will pop up; and when
 * the text box is losing focus, the keyboard will hide automatically. Set {@link setAutoHide AutoHide} to
 * false to keep the keyboard showing all the time.
 *
 * The appearance of the keyboard can also be changed by specifying a customized CSS file via
 * {@link setCssUrl CssUrl}. By default, the CSS class name for the keyboard is 'Keyboard'. This may
 * also be changed by specifying {@link setKeyboardCssClass KeyboardCssClass}.
 *
 * @author Sergey Morkovkin <sergeymorkovkin@mail.ru> and Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TKeyboard.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TKeyboard extends TWebControl
{
	/**
	 * @return string the ID path of the {@link TTextBox} control
	 */
	public function getForControl()
	{
		return $this->getViewState('ForControl','');
	}

	/**
	 * Sets the ID path of the {@link TTextBox} control.
	 * The ID path is the dot-connected IDs of the controls reaching from
	 * the keyboard's naming container to the target control.
	 * @param string the ID path
	 */
	public function setForControl($value)
	{
		$this->setViewState('ForControl', TPropertyValue::ensureString($value));
	}

	/**
	 * @return boolean whether the keyboard should be hidden when the textbox is not in focus. Defaults to true.
	 */
	public function getAutoHide()
	{
		return $this->getViewState('AutoHide', true);
	}

	/**
	 * @param boolean whether the keyboard should be hidden when the textbox is not in focus.
	 */
	public function setAutoHide($value)
	{
		$this->setViewState('AutoHide', TPropertyValue::ensureBoolean($value), true);
	}

	/**
	 * @return string the CSS class name for the keyboard <div> element. Defaults to 'Keyboard'.
	 */
	public function getKeyboardCssClass()
	{
		return $this->getViewState('KeyboardCssClass', 'Keyboard');
	}

	/**
	 * Sets a value indicating the CSS class name for the keyboard <div> element.
	 * Note, if you change this property, make sure you also supply a customized CSS file
	 * by specifying {@link setCssUrl CssUrl} which uses the new CSS class name for styling.
	 * @param string the CSS class name for the keyboard <div> element.
	 */
	public function setKeyboardCssClass($value)
	{
		$this->setViewState('KeyboardCssClass', $value, 'Keyboard');
	}

	/**
	 * @return string the URL for the CSS file to customize the appearance of the keyboard.
	 */
	public function getCssUrl()
	{
		return $this->getViewState('CssUrl', '');
	}

	/**
	 * @param string the URL for the CSS file to customize the appearance of the keyboard.
	 */
	public function setCssUrl($value)
	{
		$this->setViewState('CssUrl', $value, '');
	}

	/**
	 * Registers CSS and JS.
	 * This method is invoked right before the control rendering, if the control is visible.
	 * @param mixed event parameter
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		if($this->getPage()->getClientSupportsJavaScript())
		{
			$this->registerStyleSheet();
			$this->registerClientScript();
		}
	}

	/**
	 * Adds attribute name-value pairs to renderer.
	 * This method overrides the parent implementation with additional TKeyboard specific attributes.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		if($this->getPage()->getClientSupportsJavaScript())
			$writer->addAttribute('id',$this->getClientID());
	}

	/**
	 * Registers the CSS relevant to the TKeyboard.
	 * It will register the CSS file specified by {@link getCssUrl CssUrl}.
	 * If that is not set, it will use the default CSS.
	 */
	protected function registerStyleSheet()
	{
		if(($url=$this->getCssUrl())==='')
			$url=$this->getApplication()->getAssetManager()->publishFilePath(dirname(__FILE__).DIRECTORY_SEPARATOR.'assets'.DIRECTORY_SEPARATOR.'keyboard.css');
		$this->getPage()->getClientScript()->registerStyleSheetFile($url,$url);
	}

	/**
	 * Registers the relevant JavaScript.
	 */
	protected function registerClientScript()
	{
		$options=TJavaScript::encode($this->getClientOptions());
		$className=$this->getClientClassName();
		$cs=$this->getPage()->getClientScript();
		$cs->registerPradoScript('keyboard');
		$cs->registerEndScript('prado:'.$this->getClientID(), "new $className($options);");
	}

	/**
	 * @return string the Javascript class name for this control
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TKeyboard';
	}

	/**
	 * @return array the JavaScript options for this control
	 */
	protected function getClientOptions()
	{
		if(($forControl=$this->getForControl())==='')
			throw new TConfigurationException('keyboard_forcontrol_required');
	    if(($target=$this->findControl($forControl))===null)
	        throw new TConfigurationException('keyboard_forcontrol_invalid',$forControl);

	    $options['ID'] = $this->getClientID();
	    $options['ForControl'] = $target->getClientID();
	    $options['AutoHide'] = $this->getAutoHide();
	    $options['CssClass'] = $this->getKeyboardCssClass();

	    return $options;
	}
}

