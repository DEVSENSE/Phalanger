<?php

/**
 * TSqlMapException is the base exception class for all SqlMap exceptions.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapException.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TSqlMapException extends TException
{
	/**
	 * Constructor, similar to the parent constructor. For parameters that
	 * are of SimpleXmlElement, the tag name and its attribute names and values
	 * are expanded into a string.
	 */
	public function __construct($errorMessage)
	{
		$this->setErrorCode($errorMessage);
		$errorMessage=$this->translateErrorMessage($errorMessage);
		$args=func_get_args();
		array_shift($args);
		$n=count($args);
		$tokens=array();
		for($i=0;$i<$n;++$i)
		{
			if($args[$i] instanceof SimpleXmlElement)
				$tokens['{'.$i.'}']=$this->implodeNode($args[$i]);
			else
				$tokens['{'.$i.'}']=TPropertyValue::ensureString($args[$i]);
		}
		parent::__construct(strtr($errorMessage,$tokens));
	}

	/**
	 * @param SimpleXmlElement node
	 * @return string tag name and attribute names and values.
	 */
	protected function implodeNode($node)
	{
		$attributes=array();
		foreach($node->attributes() as $k=>$v)
			$attributes[]=$k.'="'.(string)$v.'"';
		return '<'.$node->getName().' '.implode(' ',$attributes).'>';
	}

	/**
	 * @return string path to the error message file
	 */
	protected function getErrorMessageFile()
	{
		$lang=Prado::getPreferredLanguage();
		$dir=dirname(__FILE__);
		$msgFile=$dir.'/messages-'.$lang.'.txt';
		if(!is_file($msgFile))
			$msgFile=$dir.'/messages.txt';
		return $msgFile;
	}
}

/**
 * TSqlMapConfigurationException, raised during configuration file parsing.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapException.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TSqlMapConfigurationException extends TSqlMapException
{

}

/**
 * TSqlMapUndefinedException, raised when mapped statemented are undefined.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapException.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TSqlMapUndefinedException extends TSqlMapException
{

}

/**
 * TSqlMapDuplicateException, raised when a duplicate mapped statement is found.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapException.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TSqlMapDuplicateException extends TSqlMapException
{
}

/**
 * TInvalidPropertyException, raised when setting or getting an invalid property.
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TSqlMapException.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TInvalidPropertyException extends TSqlMapException
{
}

class TSqlMapExecutionException extends TSqlMapException
{
}

