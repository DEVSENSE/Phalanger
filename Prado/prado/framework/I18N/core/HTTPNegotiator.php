<?php

/**
 * HTTPNegotiator class file.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the BSD License.
 *
 * Copyright(c) 2004 by Qiang Xue. All rights reserved.
 *
 * To contact the author write to {@link mailto:qiang.xue@gmail.com Qiang Xue}
 * The latest version of PRADO can be obtained from:
 * {@link http://prado.sourceforge.net/}
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Revision: 1.2 $  $Date: 2005/01/05 03:15:14 $
 * @package System.I18N.core
 */

/**
 * Include the CultureInfo class.
 */
require_once(dirname(__FILE__).'/CultureInfo.php');

/**
 * HTTPNegotiator class.
 *
 * Get the language and charset information from the client browser.
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version v1.0, last update on Fri Dec 24 16:01:35 EST 2004
 * @package System.I18N.core
 */
class HTTPNegotiator
{
	/**
	 * A list of languages accepted by the browser.
	 * @var array
	 */
	protected $languages;

	/**
	 * A list of charsets accepted by the browser
	 * @var array
	 */
	protected $charsets;

	/**
	 * Get a list of languages acceptable by the client browser
	 * @return array languages ordered in the user browser preferences.
	 */
	function getLanguages()
	{
		if($this->languages !== null) {
			return $this->languages;
		}

		$this->languages = array();

		if (!isset($_SERVER['HTTP_ACCEPT_LANGUAGE']))
            return $this->languages;

		//$basedir = CultureInfo::dataDir();
		//$ext = CultureInfo::fileExt();
		$info = new CultureInfo();

		foreach(explode(',', $_SERVER['HTTP_ACCEPT_LANGUAGE']) as $lang)
		{
            // Cut off any q-value that might come after a semi-colon
            if ($pos = strpos($lang, ';'))
                $lang = trim(substr($lang, 0, $pos));

			if (strstr($lang, '-'))
			{
				$codes = explode('-',$lang);
				if($codes[0] == 'i')
				{
                    // Language not listed in ISO 639 that are not variants
                    // of any listed language, which can be registerd with the
                    // i-prefix, such as i-cherokee
					if(count($codes)>1)
						$lang = $codes[1];
				}
				else
				{
					for($i = 0, $k = count($codes); $i<$k; ++$i)
					{
						if($i == 0)
							$lang = strtolower($codes[0]);
						else
							$lang .= '_'.strtoupper($codes[$i]);
					}
				}
            }



			if($info->validCulture($lang))
				$this->languages[] = $lang;
        }

		return $this->languages;
	}

	/**
	 * Get a list of charsets acceptable by the client browser.
	 * @return array list of charsets in preferable order.
	 */
	function getCharsets()
	{
        if($this->charsets !== null) {
			return $this->charsets;
		}

		$this->charsets = array();

		if (!isset($_SERVER['HTTP_ACCEPT_CHARSET']))
            return $this->charsets;

		foreach (explode(',', $_SERVER['HTTP_ACCEPT_CHARSET']) as $charset)
		{
            if (!empty($charset))
                $this->charsets[] = preg_replace('/;.*/', '', $charset);
        }

		return $this->charsets;
	}
}

