<?php
/**
 * TClientScript class file
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TClientScript.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TClientScript class
 *
 * Allows importing of Prado Client Scripts from template via the
 * {@link setPradoScripts PradoScripts} property. Multiple Prado
 * client-scripts can be specified using comma delimited string of the
 * javascript library to include on the page. For example,
 *
 * <code>
 * <com:TClientScript PradoScripts="effects, rico" />
 * </code>
 *
 * Custom javascript files can be register using the {@link setScriptUrl ScriptUrl}
 * property.
 * <code>
 * <com:TClientScript ScriptUrl=<%~ test.js %> />
 * </code>
 *
 * Contents within TClientScript will be treated as javascript code and will be
 * rendered in place.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TClientScript.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TClientScript extends TControl
{
	/**
	 * @return string comma delimited list of javascript libraries to included
	 * on the page.
	 */
	public function getPradoScripts()
	{
		return $this->getViewState('PradoScripts', '');
	}

	/**
	 * Include javascript library to the current page. The current supported
	 * libraries are: "prado", "effects", "ajax", "validator", "logger",
	 * "datepicker", "colorpicker". Library dependencies are automatically resolved.
	 *
	 * @param string comma delimited list of javascript libraries to include.
	 */
	public function setPradoScripts($value)
	{
		$this->setViewState('PradoScripts', $value, '');
	}

	/**
	 * @return string custom javascript file url.
	 */
	public function getScriptUrl()
	{
		return $this->getViewState('ScriptUrl', '');
	}

	/**
	 * @param string custom javascript file url.
	 */
	public function setScriptUrl($value)
	{
		$this->setViewState('ScriptUrl', $value, '');
	}

	/**
	 * Calls the client script manager to add each of the requested client
	 * script libraries.
	 * @param mixed event parameter
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		$scripts = preg_split('/,|\s+/', $this->getPradoScripts());
		$cs = $this->getPage()->getClientScript();
		foreach($scripts as $script)
		{
			if(($script = trim($script))!=='')
				$cs->registerPradoScript($script);
		}
	}

	/**
	 * Renders the body content as javascript block.
	 * Overrides parent implementation, parent renderChildren method is called during
	 * {@link registerCustomScript}.
	 * @param THtmlWriter the renderer
	 */
	public function render($writer)
	{
		$this->renderCustomScriptFile($writer);
		$this->renderCustomScript($writer);
	}

	/**
	 * Renders the custom script file.
	 * @param THtmLWriter the renderer
	 */
	protected function renderCustomScriptFile($writer)
	{
		if(($scriptUrl = $this->getScriptUrl())!=='')
			$writer->write("<script type=\"text/javascript\" src=\"$scriptUrl\"></script>\n");
	}

	/**
	 * Registers the body content as javascript.
	 * @param THtmlWriter the renderer
	 */
	protected function renderCustomScript($writer)
	{
		if($this->getHasControls())
		{
			$writer->write("<script type=\"text/javascript\">\n/*<![CDATA[*/\n");
			$this->renderChildren($writer);
			$writer->write("\n/*]]>*/\n</script>\n");
		}
	}
}

