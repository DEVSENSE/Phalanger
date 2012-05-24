<?php
/**
 * PRADO Requirements Checker script
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: index.php 2923 2011-05-22 14:43:11Z ctrlaltca@gmail.com $
 * @package prado
 */

/**
 * PRADO Requirements Checker script
 *
 * This script will check if your system meets the requirements for running PRADO.
 * It will check if you are running the right version of PHP, if you included
 * the right libraries and if your php.ini file settings are correct.
 *
 * This script is capable of displaying localized messages.
 * All messages are stored in messages.txt. A localized message file is named as
 * messsages-<language code>.txt, and it will be used when the client browser
 * chooses the corresponding language.
 * The script output uses a template named template.html.
 * Its localized version is stored in template-<language code>.html.
 */

// TO BE CONFIRMED: PHP 5.1.0 has problem with I18N and L10N
/**
 * @var array List of requirements (required or not, check item, hint)
 */
$requirements = array(
	array(
		true,
		version_compare(PHP_VERSION,"5.1.0",">="),
		'PHP version check','PHP 5.1.0 or higher required'),
	array(
		true,
		isset($_SERVER["HTTP_ACCEPT"]),
		'$_SERVER["HTTP_ACCEPT"] check',
		'HTTP_ACCEPT required'),
	array(
		true,
		isset($_SERVER["SCRIPT_FILENAME"]) && realpath($_SERVER["SCRIPT_FILENAME"])===realpath(__FILE__),
		'$_SERVER["SCRIPT_FILENAME"] check',
		'SCRIPT_FILENAME required'),
	array(
		true,
		isset($_SERVER["REQUEST_URI"]) || isset($_SERVER["QUERY_STRING"]),
		'$_SERVER["REQUEST_URI"] check',
		'REQUEST_URI required'),
	array(
		true,
		isset($_SERVER["PATH_INFO"]) || strpos($_SERVER["PHP_SELF"],$_SERVER["SCRIPT_NAME"])===0,
		'$_SERVER["PATH_INFO"] check',
		'PATH_INFO required'),
	array(
		true,
		class_exists('Reflection',false),
		'Reflection extension check',
		'Reflection extension required'),
	array(
		true,
		class_exists("DOMDocument",false),
		'DOM extension check',
		'DOM extension required'),
    array(
        true,
        extension_loaded("SPL"),
        'SPL extension check',
        'SPL extension required'),
    array(
        true,
        extension_loaded("CType"),
        'CType extension check',
        'CType extension required'),
    array(
        true,
        extension_loaded("pcre"),
        'PCRE extension check',
        'PCRE extension required'),
    array(
        false,
        class_exists("PDO",false),
        'PDO extension check',
        'PDO extension optional'),
	array(
		false,
		function_exists("iconv"),
		'ICONV extension check',
		'ICONV extension optional'),
	array(
		false,
		extension_loaded("zlib"),
		'Zlib extension check',
		'Zlib extension optional'),
	array(
		false,
		extension_loaded("sqlite"),
		'SQLite extension check',
		'SQLite extension optional'),
	array(
		false,
		extension_loaded("memcache"),
		'Memcache extension check',
		'Memcache extension optional'),
	array(
		false,
		extension_loaded("apc"),
		'APC extension check',
		'APC extension optional'),
	array(
		false,
		extension_loaded("mcrypt"),
		'Mcrypt extension check',
		'Mcrypt extension optional'),
	array(
		false,
		extension_loaded("xsl"),
		'XSL extension check',
		'XSL extension optional'),
	array(
		false,
		extension_loaded("soap"),
		'SOAP extension check',
		'SOAP extension optional'),
);

$results = "<table class=\"result\">\n";
$conclusion = 0;
foreach($requirements as $requirement)
{
	list($required,$expression,$aspect,$hint)=$requirement;
	//eval('$ret='.$expression.';');
	$ret=$expression;
	if($required)
	{
		if($ret)
			$ret='passed';
		else
		{
			$conclusion=1;
			$ret='error';
		}
	}
	else
	{
		if($ret)
			$ret='passed';
		else
		{
			if($conclusion!==1)
				$conclusion=2;
			$ret='warning';
		}
	}
	$results.="<tr class=\"$ret\"><td class=\"$ret\">".lmessage($aspect)."</td><td class=\"$ret\">".lmessage($hint)."</td></tr>\n";
}
$results .= '</table>';
if($conclusion===0)
	$conclusion=lmessage('all passed');
else if($conclusion===1)
	$conclusion=lmessage('failed');
else
	$conclusion=lmessage('passed with warnings');

$tokens = array(
	'%%Conclusion%%' => $conclusion,
	'%%Details%%' => $results,
	'%%Version%%' => $_SERVER['SERVER_SOFTWARE'].' <a href="http://www.pradosoft.com/">PRADO</a>/'.getPradoVersion(),
	'%%Time%%' => @strftime('%Y-%m-%d %H:%m',time()),
);

$lang=getPreferredLanguage();
$templateFile=dirname(__FILE__)."/template-$lang.html";
if(!is_file($templateFile))
	$templateFile=dirname(__FILE__).'/template.html';
if(($content=@file_get_contents($templateFile))===false)
	die("Unable to open template file '$templateFile'.");

header('Content-Type: text/html; charset=UTF-8');
echo strtr($content,$tokens);

/**
 * Returns a localized message according to user preferred language.
 * @param string message to be translated
 * @return string translated message
 */
function lmessage($token)
{
	static $messages=null;
	if($messages===null)
	{
		$lang = getPreferredLanguage();
		$msgFile=dirname(__FILE__)."/messages-$lang.txt";
		if(!is_file($msgFile))
			$msgFile=dirname(__FILE__).'/messages.txt';
		if(($entries=@file($msgFile))!==false)
		{
			foreach($entries as $entry)
			{
				@list($code,$message)=explode('=',$entry,2);
				$messages[trim($code)]=trim($message);
			}
		}
	}
	return isset($messages[$token])?$messages[$token]:$token;
}

/**
 * Returns a list of user preferred languages.
 * The languages are returned as an array. Each array element
 * represents a single language preference. The languages are ordered
 * according to user preferences. The first language is the most preferred.
 * @return array list of user preferred languages.
 */
function getUserLanguages()
{
	static $languages=null;
	if($languages===null)
	{
		$languages=array();
		foreach(explode(',',$_SERVER['HTTP_ACCEPT_LANGUAGE']) as $language)
		{
			$array=explode(';q=',trim($language));
			$languages[trim($array[0])]=isset($array[1])?(float)$array[1]:1.0;
		}
		arsort($languages);
		$languages=array_keys($languages);
		if(empty($languages))
			$languages[0]='en';
	}
	return $languages;
}

/**
 * Returns the most preferred language by the client user.
 * @return string the most preferred language by the client user, defaults to English.
 */
function getPreferredLanguage()
{
	static $language=null;
	if($language===null)
	{
		$langs=getUserLanguages();
		$lang=explode('-',$langs[0]);
		if(empty($lang[0]) || !function_exists('ctype_alpha') || !ctype_alpha($lang[0]))
			$language='en';
		else
			$language=$lang[0];
	}
	return $language;
}

/**
 * @return string Prado version
 */
function getPradoVersion()
{
	$coreFile=dirname(__FILE__).'/../framework/PradoBase.php';
	if(is_file($coreFile))
	{
		$contents=file_get_contents($coreFile);
		$matches=array();
		if(preg_match('/public static function getVersion.*?return \'(.*?)\'/ms',$contents,$matches)>0)
			return $matches[1];
	}
	return '';
}

?>