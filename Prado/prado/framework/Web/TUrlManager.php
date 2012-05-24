<?php
/**
 * TUrlManager class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id $
 * @package System.Web
 */

/**
 * TUrlManager class
 *
 * TUrlManager is the base class for managing URLs that can be
 * recognized by PRADO applications. It provides the default implementation
 * for parsing and constructing URLs.
 *
 * Derived classes may override {@link constructUrl} and {@link parseUrl}
 * to provide customized URL schemes.
 *
 * By default, {@link THttpRequest} uses TUrlManager as its URL manager.
 * If you want to use your customized URL manager, load your manager class
 * as an application module and set {@link THttpRequest::setUrlManager THttpRequest.UrlManager}
 * with the ID of your URL manager module.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id $
 * @package System.Web
 * @since 3.0.6
 */
class TUrlManager extends TModule
{
	/**
	 * Constructs a URL that can be recognized by PRADO.
	 *
	 * This method provides the actual implementation used by {@link THttpRequest::constructUrl}.
	 * Override this method if you want to provide your own way of URL formatting.
	 * If you do so, you may also need to override {@link parseUrl} so that the URL can be properly parsed.
	 *
	 * The URL is constructed as the following format:
	 * /entryscript.php?serviceID=serviceParameter&get1=value1&...
	 * If {@link THttpRequest::setUrlFormat THttpRequest.UrlFormat} is 'Path',
	 * the following format is used instead:
	 * /entryscript.php/serviceID/serviceParameter/get1,value1/get2,value2...
	 * @param string service ID
	 * @param string service parameter
	 * @param array GET parameters, null if not provided
	 * @param boolean whether to encode the ampersand in URL
	 * @param boolean whether to encode the GET parameters (their names and values)
	 * @return string URL
	 * @see parseUrl
	 */
	public function constructUrl($serviceID,$serviceParam,$getItems,$encodeAmpersand,$encodeGetItems)
	{
		$url=$serviceID.'='.urlencode($serviceParam);
		$amp=$encodeAmpersand?'&amp;':'&';
		$request=$this->getRequest();
		if(is_array($getItems) || $getItems instanceof Traversable)
		{
			if($encodeGetItems)
			{
				foreach($getItems as $name=>$value)
				{
					if(is_array($value))
					{
						$name=urlencode($name.'[]');
						foreach($value as $v)
							$url.=$amp.$name.'='.urlencode($v);
					}
					else
						$url.=$amp.urlencode($name).'='.urlencode($value);
				}
			}
			else
			{
				foreach($getItems as $name=>$value)
				{
					if(is_array($value))
					{
						foreach($value as $v)
							$url.=$amp.$name.'[]='.$v;
					}
					else
						$url.=$amp.$name.'='.$value;
				}
			}
		}
		if($request->getUrlFormat()===THttpRequestUrlFormat::Path)
			return $request->getApplicationUrl().'/'.strtr($url,array($amp=>'/','?'=>'/','='=>$request->getUrlParamSeparator()));
		else
			return $request->getApplicationUrl().'?'.$url;
	}

	/**
	 * Parses the request URL and returns an array of input parameters.
	 * This method is automatically invoked by {@link THttpRequest} when
	 * handling a user request.
	 *
	 * In general, this method should parse the path info part of the requesting URL
	 * and generate an array of name-value pairs according to some scheme.
	 * The current implementation deals with both 'Get' and 'Path' URL formats.
	 *
	 * You may override this method to support customized URL format.
	 * @return array list of input parameters, indexed by parameter names
	 * @see constructUrl
	 */
	public function parseUrl()
	{
		$request=$this->getRequest();
		$pathInfo=trim($request->getPathInfo(),'/');
		if($request->getUrlFormat()===THttpRequestUrlFormat::Path && $pathInfo!=='')
		{
			$separator=$request->getUrlParamSeparator();
			$paths=explode('/',$pathInfo);
			$getVariables=array();
			foreach($paths as $path)
			{
				if(($path=trim($path))!=='')
				{
					if(($pos=strpos($path,$separator))!==false)
					{
						$name=substr($path,0,$pos);
						$value=substr($path,$pos+1);
						if(($pos=strpos($name,'[]'))!==false)
							$getVariables[substr($name,0,$pos)][]=$value;
						else
							$getVariables[$name]=$value;
					}
					else
						$getVariables[$path]='';
				}
			}
			return $getVariables;
		}
		else
			return array();
	}
}

