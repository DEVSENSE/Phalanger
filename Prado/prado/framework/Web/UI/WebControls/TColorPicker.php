<?php
/**
 * TColorPicker class file
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TColorPicker.php 2942 2011-06-01 19:49:56Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 */

/**
 * TColorPicker class.
 *
 * TColorPicker displays a text box for color input purpose.
 * Next to the textbox there's a button filled with the current chosen color.
 * Users can write a color name directly in the text box as an hex triplet (also known as HTML notation, eg: #FF00FF).
 * Alternatively, if the <b>ShowColorPicker</b> property is enabled (it is by default), users can click the button
 * to have a color picker UI appear. A color chan be chosen directly by clicking on the color picker.
 *
 * TColorPicker has three different color picker UI <b>Mode</b>s:
 *  # <b>Simple</b> - Grid with 12 simple colors.
 *  # <b>Basic</b> - Grid with the most common 70 colors. This is the default mode.
 *  # <b>Full</b> - Full-featured color picker.
 * 
 * The <b>CssClass</b> property can be used to override the CSS class name
 * for the color picker panel. The <b>ColorStyle</b> property sets the packages
 * styles available. E.g. <b>default</b>.
 * 
 * If the <b>Mode</b> property is set to <b>Full</b>, the color picker panel will
 * display an "Ok" and "Cancel" buttons. You can customize the button labels setting the <b>OKButtonText</b>
 * and <b>CancelButtonText</b> properties.
 * 
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TColorPicker.php 2942 2011-06-01 19:49:56Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TColorPicker extends TTextBox
{
	const SCRIPT_PATH = 'prado/colorpicker';

	private $_clientSide;

	/**
	 * @return boolean whether the color picker should pop up when the button is clicked.
	 */
	public function getShowColorPicker()
	{
		return $this->getViewState('ShowColorPicker',true);
	}

	/**
	 * Sets whether to pop up the color picker when the button is clicked.
	 * @param boolean whether to show the color picker popup
	 */
	public function setShowColorPicker($value)
	{
		$this->setViewState('ShowColorPicker',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @param TColorPickerMode color picker UI mode
	 */
	public function setMode($value)
	{
	   $this->setViewState('Mode', TPropertyValue::ensureEnum($value, 'TColorPickerMode'), TColorPickerMode::Basic);
	}

	/**
	 * @return TColorPickerMode current color picker UI mode. Defaults to TColorPickerMode::Basic.
	 */
	public function getMode()
	{
	   return $this->getViewState('Mode', TColorPickerMode::Basic);
	}

	/**
	 * @param string set the color picker style
	 */
	public function setColorPickerStyle($value)
	{
	   $this->setViewState('ColorStyle', $value, 'default');
	}

	/**
	 * @return string current color picker style
	 */
	public function getColorPickerStyle()
	{
	   return $this->getViewState('ColorStyle', 'default');
	}

	/**
	 * @return string text for the color picker OK button. Default is "OK".
	 */
	public function getOKButtonText()
	{
		return $this->getViewState('OKButtonText', 'OK');
	}

	/**
	 * @param string text for the color picker OK button
	 */
	public function setOKButtonText($value)
	{
		$this->setViewState('OKButtonText', $value, 'OK');
	}

	/**
	 * @return string text for the color picker Cancel button. Default is "Cancel".
	 */
	public function getCancelButtonText()
	{
		return $this->getViewState('CancelButtonText', 'Cancel');
	}

	/**
	 * @param string text for the color picker Cancel button
	 */
	public function setCancelButtonText($value)
	{
		$this->setViewState('CancelButtonText', $value, 'Cancel');
	}

	/**
	 * @return TColorPickerClientSide javascript event options.
	 */
	public function getClientSide()
	{
		if($this->_clientSide===null)
			$this->_clientSide = $this->createClientSide();
		return $this->_clientSide;
	}

	/**
	 * @return TColorPickerClientSide javascript validator event options.
	 */
	protected function createClientSide()
	{
		return new TColorPickerClientSide;
	}

	/**
	 * Get javascript color picker options.
	 * @return array color picker client-side options
	 */
	protected function getPostBackOptions()
	{
		$options = parent::getPostBackOptions();
		$options['ClassName'] = $this->getCssClass();
		$options['ShowColorPicker'] = $this->getShowColorPicker();
		if($options['ShowColorPicker'])
		{
			$mode = $this->getMode();
			if($mode == TColorPickerMode::Full) $options['Mode'] = $mode;
			else if($mode == TColorPickerMode::Simple) $options['Palette'] = 'Tiny';
			$options['OKButtonText'] = $this->getOKButtonText();
			$options['CancelButtonText'] = $this->getCancelButtonText();
		}
		$options = array_merge($options,$this->getClientSide()->getOptions()->toArray());
		return $options;
	}

	/**
	 * @param string asset file in the self::SCRIPT_PATH directory.
	 * @return string asset file url.
	 */
	protected function getAssetUrl($file='')
	{
		$base = $this->getPage()->getClientScript()->getPradoScriptAssetUrl();
		return $base.'/'.self::SCRIPT_PATH.'/'.$file;
	}

	/**
	 * Publish the color picker Css asset files.
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		$this->publishColorPickerAssets();
	}

	/**
	 * Publish the color picker assets.
	 */
	protected function publishColorPickerAssets()
	{
		$cs = $this->getPage()->getClientScript();
		$key = "prado:".get_class($this);
		$imgs['button.gif'] = $this->getAssetUrl('button.gif');
		$imgs['background.png'] = $this->getAssetUrl('background.png');
		$options = TJavaScript::encode($imgs);
		$code = "Prado.WebUI.TColorPicker.UIImages = {$options};";
		$cs->registerEndScript($key, $code);
		$cs->registerPradoScript("colorpicker");
		$url = $this->getAssetUrl($this->getColorPickerStyle().'.css');
		if(!$cs->isStyleSheetFileRegistered($url))
			$cs->registerStyleSheetFile($url, $url);
	}

	/**
	 * Renders additional body content.
	 * This method overrides parent implementation by adding
	 * additional color picker button.
	 * @param THtmlWriter writer
	 */
	public function renderEndTag($writer)
	{
		parent::renderEndTag($writer);

		$color = $this->getText();
		$writer->addAttribute('class', 'TColorPicker_button');
		$writer->renderBeginTag('span');

		$writer->addAttribute('id', $this->getClientID().'_button');
		$writer->addAttribute('src', $this->getAssetUrl('button.gif'));
		if($color !== '')
			$writer->addAttribute('style', "background-color:{$color};");
		$writer->addAttribute('width', '20');
		$writer->addAttribute('height', '20');
		$writer->addAttribute('alt', '');
		$writer->renderBeginTag('img');
		$writer->renderEndTag();
		$writer->renderEndTag();
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TColorPicker';
	}
}

/**
 * TColorPickerMode class.
 * TColorPickerMode defines the enumerable type for the possible UI mode
 * that a {@link TColorPicker} control can take.
 *
 * The following enumerable values are defined:
 * # Simple - Grid with 12 simple colors.
 * # Basic - Grid with the most common 70 colors. This is the default mode.
 * # Full - Full-featured color picker.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TColorPicker.php 2942 2011-06-01 19:49:56Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.0.4
 */
class TColorPickerMode extends TEnumerable
{
	const Simple='Simple';
	const Basic='Basic';
	const Full='Full';
}

/**
 * TColorPickerClientSide class.
 *
 * Client-side javascript code options.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TColorPicker.php 2942 2011-06-01 19:49:56Z ctrlaltca@gmail.com $
 * @package System.Web.UI.WebControls
 * @since 3.1
 */
class TColorPickerClientSide extends TClientSideOptions
{
	/**
	 * @return string javascript code for when a color is selected.
	 */
	public function getOnColorSelected()
	{
		return $this->getOption('OnColorSelected');
	}

	/**
	 * @param string javascript code for when a color is selected.
	 */
	public function setOnColorSelected($javascript)
	{
		$this->setFunction('OnColorSelected', $javascript);
	}
}

