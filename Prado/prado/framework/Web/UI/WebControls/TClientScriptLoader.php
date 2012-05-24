<?php
/**
 * TClientScriptLoader class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TClientScriptLoader.php 1827 2007-04-02 06:19:55Z wei $
 * @package System.Web.UI.WebControls
 */

/**
 * The TClientScriptLoader publish a collection of javascript files as assets.
 * The {@link PackagePath setPackagePath} property can be an existing asset directory
 * or a namespace path to the directory containing javascript files. E.g.
 * <code>
 *   <com:TClientScriptLoader PackagePath=<%~ mylib/js %> />
 *   <com:TClientScriptLoader PackagePath="Application.myscripts" />
 * </code>
 *
 * When the files in the {@link PackagePath setPackagePath} are published as assets, a script loader
 * php file corresponding to TClientScriptManager::SCRIPT_LOADER is also copied to that asset directory.
 *
 * The script loader, combines multiple javascript files and serve up as gzip if possible.
 * Allowable scripts and script dependencies can be specified in a "packages.php" file
 * with the following format. This "packages.php" is optional, if absent the filenames
 * without ".js" extension are used. The "packages.php" must be in the directory given by
 * {@link PackagePath setPackagePath}.
 *
 * <code>
 * <?php
 *  $packages = array(
 *     'package1' => array('file1.js', 'file2.js'),
 *     'package2' => array('file3.js', 'file4.js'));
 *
 *  $deps = array(
 *     'package1' => array('package1'),
 *     'package2' => array('package1', 'package2')); //package2 requires package1 first.
 *
 *  return array($packages,$deps); //must return $packages and $deps in an array
 * </code>
 *
 * Set the {@link PackageScripts setPackageScripts} property with value 'package1' to serve
 * up the 'package1' scripts. A maxium of 25 packages separated by commas is allowed.
 *
 * Dependencies of the packages are automatically resolved by the script loader php file.
 * E.g.
 * <code>
 * <com:TClientScriptLoader PackagePath=<%~ mylib/js %> PackageScripts="package2" />
 * </code>
 *
 * The {@link setDebugMode DebugMode} property when false
 * removes comments and whitespaces from the published javascript files. If
 * the DebugMode property is not set, the debug mode is determined from the application mode.
 *
 * The {@link setEnableGzip EnableGzip} property (default is true) enables the
 * published javascripts to be served as zipped if the browser and php server allows it.
 *
 * If the DebugMode is false either explicitly or when the application mode is non-debug,
 * then cache headers are also sent to inform the browser and proxies to cache the file.
 * Moreover, the post-processed (comments removed and zipped) are saved in the assets
 * directory for the next requests. That is, in non-debug mode the scripts are cached
 * in the assets directory until they are deleted.
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @version $Id$
 * @package System.Web.UI.WebControls
 * @since 3.1
 */
class TClientScriptLoader extends TWebControl
{
	/**
	 * @return string tag name of the script element
	 */
	protected function getTagName()
	{
		return 'script';
	}

	/**
	 * Adds attribute name-value pairs to renderer.
	 * This overrides the parent implementation with additional button specific attributes.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		$writer->addAttribute('type','text/javascript');
		$writer->addAttribute('src',$this->getClientScriptUrl());
		parent::addAttributesToRender($writer);
	}

	/**
	 * @return string clientscript.php url.
	 */
	protected function getClientScriptUrl()
	{
		$scripts = preg_split('/\s*[, ]+\s*/', $this->getPackageScripts());
		$cs = $this->getPage()->getClientScript();
		return $cs->registerJavascriptPackages($this->getPackagePath(),
				$scripts, $this->getDebugMode(), $this->getEnableGzip());
	}

	/**
	 * @param string custom javascript library directory.
	 */
	public function setPackagePath($value)
	{
		$this->setViewState('PackagePath', $value);
	}

	/**
	 * @return string custom javascript library directory.
	 */
	public function getPackagePath()
	{
		return $this->getViewState('PackagePath');
	}

	/**
	 * @param string load specific packages from the javascript library in the PackagePath,
	 * comma delimited package names. A maximum of 25 packages is allowed.
	 */
	public function setPackageScripts($value)
	{
		$this->setViewState('PackageScripts', $value,'');
	}

	/**
	 * @return string comma delimited list of javascript library packages to load.
	 */
	public function getPackageScripts()
	{
		return $this->getViewState('PackageScripts','');
	}

	/**
	 * @param boolean enables gzip compression of the javascript.
	 */
	public function setEnableGzip($value)
	{
		$this->setViewState('EnableGzip', TPropertyValue::ensureBoolean($value), true);
	}

	/**
	 * @return boolean enables gzip compression of the javascript if possible, default is true.
	 */
	public function getEnableGzip()
	{
		return $this->getViewState('EnableGzip', true);
	}

	/**
	 * @return boolean javascript comments stripped in non-debug mode.
	 * Debug mode will depend on the application mode if null.
	 */
	public function getDebugMode()
	{
		return $this->getViewState('DebugMode');
	}

	/**
	 * @param boolean true to enable debug mode, default is null thus dependent on the application mode.
	 */
	public function setDebugMode($value)
	{
		$this->setViewState('DebugMode', TPropertyValue::ensureBoolean($value), null);
	}
}

