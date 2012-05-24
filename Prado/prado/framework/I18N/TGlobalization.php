<?php
/**
 * TGlobalization class file.
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TGlobalization.php 2936 2011-06-01 07:20:38Z ctrlaltca@gmail.com $
 * @package System.I18N
 */


/**
 * TGlobalization contains settings for Culture, Charset
 * and TranslationConfiguration.
 *
 * TGlobalization can be subclassed to change how the Culture, Charset
 * are determined. See TGlobalizationAutoDetect for example of
 * setting the Culture based on browser settings.
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @version $Revision: 1.66 $  $Date: ${DATE} ${TIME} $
 * @package System.I18N
 * @since 3.0
 */
class TGlobalization extends TModule
{
	/**
	 * Default character set is 'UTF-8'.
	 * @var string
	 */
	private $_defaultCharset = 'UTF-8';

	/**
	 * Default culture is 'en'.
	 * @var string
	 */
	private $_defaultCulture = 'en';

	/**
	 * The current charset.
	 * @var string
	 */
	private $_charset=null;

	/**
	 * The current culture.
	 * @var string
	 */
	private $_culture=null;

	/**
	 * Translation source parameters.
	 * @var TMap
	 */
	private $_translation;

	/**
	 * @var boolean whether we should translate the default culture
	 */
	private $_translateDefaultCulture=true;

	/**
	 * Initialize the Culture and Charset for this application.
	 * You should override this method if you want a different way of
	 * setting the Culture and/or Charset for your application.
	 * If you override this method, call parent::init($xml) first.
	 * @param TXmlElement application configuration
	 */
	public function init($xml)
	{
		if($this->_charset===null)
			$this->_charset=$this->getDefaultCharset();
		if($this->_culture===null)
			$this->_culture=$this->getDefaultCulture();

		if($xml!==null)
		{
			$translation = $xml->getElementByTagName('translation');
			if($translation)
				$this->setTranslationConfiguration($translation->getAttributes());
		}
		$this->getApplication()->setGlobalization($this);
	}

	/**
	 * @return string default culture
	 */
	public function getTranslateDefaultCulture()
	{
		return $this->_translateDefaultCulture;
	}

	/**
	 * @param bool default culture, e.g. <tt>en_US</tt> for American English
	 */
	public function setTranslateDefaultCulture($value)
	{
		$this->_translateDefaultCulture = TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return string default culture
	 */
	public function getDefaultCulture()
	{
		return $this->_defaultCulture;
	}

	/**
	 * @param string default culture, e.g. <tt>en_US</tt> for American English
	 */
	public function setDefaultCulture($culture)
	{
		$this->_defaultCulture = str_replace('-','_',$culture);
	}

	/**
	 * @return string default charset set
	 */
	public function getDefaultCharset()
	{
		return $this->_defaultCharset;
	}

	/**
	 * @param string default localization charset, e.g. <tt>UTF-8</tt>
	 */
	public function setDefaultCharset($charset)
	{
		$this->_defaultCharset = $charset;
	}

	/**
	 * @return string current application culture
	 */
	public function getCulture()
	{
		return $this->_culture;
	}

	/**
	 * @param string culture, e.g. <tt>en_US</tt> for American English
	 */
	public function setCulture($culture)
	{
		$this->_culture = str_replace('-','_',$culture);
	}

	/**
	 * @return string localization charset
	 */
	public function getCharset()
	{
		return $this->_charset;
	}

	/**
	 * @param string localization charset, e.g. <tt>UTF-8</tt>
	 */
	public function setCharset($charset)
	{
		$this->_charset = $charset;
	}

	/**
	 * @return TMap translation source configuration.
	 */
	public function getTranslationConfiguration()
	{
		return (!$this->_translateDefaultCulture && ($this->getDefaultCulture() == $this->getCulture()))
			? null
			: $this->_translation;
	}

	/**
	 * Sets the translation configuration. Example configuration:
	 * <code>
	 * $config['type'] = 'XLIFF'; //XLIFF, gettext, Database or MySQL (deprecated)
	 * $config['source'] = 'Path.to.directory'; // for types XLIFF and gettext
	 * $config['source'] = 'connectionId'; // for type Database
	 * $config['source'] = 'mysql://user:pw@host/db'; // for type MySQL (deprecated)
	 * $config['catalogue'] = 'messages'; //default catalog
	 * $config['autosave'] = 'true'; //save untranslated message
	 * $config['cache'] = 'true'; //cache translated message
	 * $config['marker'] = '@@'; // surround untranslated text with '@@'
	 * </code>
	 * Throws exception is source is not found.
	 * @param TMap configuration options
	 */
	protected function setTranslationConfiguration(TMap $config)
	{
		if($config['type'] == 'XLIFF' || $config['type'] == 'gettext')
		{
			if($config['source'])
			{
				$config['source'] = Prado::getPathOfNamespace($config['source']);
				if(!is_dir($config['source']))
				{
					if(@mkdir($config['source'])===false)
					throw new TConfigurationException('globalization_source_path_failed',
						$config['source']);
					chmod($config['source'], PRADO_CHMOD); //make it deletable
				}
			}
			else
			{
				throw new TConfigurationException("invalid source dir '{$config['source']}'");
			}
		}
		if($config['cache'])
		{
			$config['cache'] = $this->getApplication()->getRunTimePath().'/i18n';
			if(!is_dir($config['cache']))
			{
				if(@mkdir($config['cache'])===false)
					throw new TConfigurationException('globalization_cache_path_failed',
						$config['cache']);
				chmod($config['cache'], PRADO_CHMOD); //make it deletable
			}
		}
		$this->_translation = $config;
	}

	/**
	 * @return string current translation catalogue.
	 */
	public function getTranslationCatalogue()
	{
		return $this->_translation['catalogue'];
	}

	/**
	 * @param string update the translation catalogue.
	 */
	public function setTranslationCatalogue($value)
	{
		$this->_translation['catalogue'] = $value;
	}

	/**
	 * Gets all the variants of a specific culture. If the parameter
	 * $culture is null, the current culture is used.
	 * @param string $culture the Culture string
	 * @return array variants of the culture.
	 */
	public function getCultureVariants($culture=null)
	{
		if($culture===null) $culture = $this->getCulture();
		$variants = explode('_', $culture);
		$result = array();
		for(; count($variants) > 0; array_pop($variants))
			$result[] = implode('_', $variants);
		return $result;
	}

	/**
	 * Returns a list of possible localized files. Example
	 * <code>
	 * $files = $app->getLocalizedResource("path/to/Home.page","en_US");
	 * </code>
	 * will return
	 * <pre>
	 * array
	 *   0 => 'path/to/en_US/Home.page'
	 *   1 => 'path/to/en/Home.page'
	 *   2 => 'path/to/Home.en_US.page'
	 *   3 => 'path/to/Home.en.page'
	 *   4 => 'path/to/Home.page'
	 * </pre>
	 * Note that you still need to verify the existance of these files.
	 * @param string filename
	 * @param string culture string, null to use current culture
	 * @return array list of possible localized resource files.
	 */
	public function getLocalizedResource($file,$culture=null)
	{
		$files = array();
		$variants = $this->getCultureVariants($culture);
		$path = pathinfo($file);
		foreach($variants as $variant)
			$files[] = $path['dirname'].DIRECTORY_SEPARATOR.$variant.DIRECTORY_SEPARATOR.$path['basename'];
		$filename = substr($path['basename'],0,strrpos($path['basename'],'.'));
		foreach($variants as $variant)
			$files[] = $path['dirname'].DIRECTORY_SEPARATOR.$filename.'.'.$variant.'.'.$path['extension'];
		$files[] = $file;
		return $files;
	}

}

?>
