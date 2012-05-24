<?php
/**
 * TMultiView and TView class file.
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Revision: 1.66 $  $Date: ${DATE} ${TIME} $
 * @package System.I18N
 */

/**
 * Import the HTTPNeogtiator
 */
Prado::using('System.I18N.core.HTTPNegotiator');

/**
 * TGlobalizationAutoDetect class will automatically try to resolve the default
 * culture using the user browser language settings.
 *
 * @author Wei Zhuo<weizhuo[at]gmail[dot]com>
 * @version $Revision: 1.66 $  $Date: ${DATE} ${TIME} $
 * @package System.I18N
 */
class TGlobalizationAutoDetect extends TGlobalization
{
	private $_detectedLanguage;

	public function init($xml)
	{
		parent::init($xml);

		//set the culture according to browser language settings
		$http = new HTTPNegotiator();		
		$languages = $http->getLanguages();
		if(count($languages) > 0)
		{
			$this->_detectedLanguage=$languages[0];
			$this->setCulture($languages[0]);
		}
	}

	public function getDetectedLanguage()
	{
		return $this->_detectedLanguage;
	}
}

