<?php
/**
 * TUrlMapping and TUrlMappingPattern class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TUrlMapping.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Web
 */

Prado::using('System.Web.TUrlManager');
Prado::using('System.Collections.TAttributeCollection');

/**
 * TUrlMapping Class
 *
 * The TUrlMapping module allows PRADO to construct and recognize URLs
 * based on specific patterns.
 *
 * TUrlMapping consists of a list of URL patterns which are used to match
 * against the currently requested URL. The first matching pattern will then
 * be used to decompose the URL into request parameters (accessible through
 * <code>$this->Request['paramname']</code>).
 *
 * The patterns can also be used to construct customized URLs. In this case,
 * the parameters in an applied pattern will be replaced with the corresponding
 * GET variable values.
 *
 * Since it is derived from {@link TUrlManager}, it should be configured globally
 * in the application configuration like the following,
 * <code>
 *  <module id="request" class="THttpRequest" UrlManager="friendly-url" />
 *  <module id="friendly-url" class="System.Web.TUrlMapping" EnableCustomUrl="true">
 *    <url ServiceParameter="Posts.ViewPost" pattern="post/{id}/" parameters.id="\d+" />
 *    <url ServiceParameter="Posts.ListPost" pattern="archive/{time}/" parameters.time="\d{6}" />
 *    <url ServiceParameter="Posts.ListPost" pattern="category/{cat}/" parameters.cat="\d+" />
 *  </module>
 * </code>
 *
 * In the above, each <tt>&lt;url&gt;</tt> element specifies a URL pattern represented
 * as a {@link TUrlMappingPattern} internally. You may create your own pattern classes
 * by extending {@link TUrlMappingPattern} and specifying the <tt>&lt;class&gt;</tt> attribute
 * in the element.
 *
 * The patterns can be also be specified in an external file using the {@link setConfigFile ConfigFile} property.
 *
 * The URL mapping are evaluated in order, only the first mapping that matches
 * the URL will be used. Cascaded mapping can be achieved by placing the URL mappings
 * in particular order. For example, placing the most specific mappings first.
 *
 * Only the PATH_INFO part of the URL is used to match the available patterns. The matching
 * is strict in the sense that the whole pattern must match the whole PATH_INFO of the URL.
 *
 * From PRADO v3.1.1, TUrlMapping also provides support for constructing URLs according to
 * the specified pattern. You may enable this functionality by setting {@link setEnableCustomUrl EnableCustomUrl} to true.
 * When you call THttpRequest::constructUrl() (or via TPageService::constructUrl()),
 * TUrlMapping will examine the available URL mapping patterns using their {@link TUrlMappingPattern::getServiceParameter ServiceParameter}
 * and {@link TUrlMappingPattern::getPattern Pattern} properties. A pattern is applied if its
 * {@link TUrlMappingPattern::getServiceParameter ServiceParameter} matches the service parameter passed
 * to constructUrl() and every parameter in the {@link getPattern Pattern} is found
 * in the GET variables.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TUrlMapping.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Web
 * @since 3.0.5
 */
class TUrlMapping extends TUrlManager
{
	/**
	 * File extension of external configuration file
	 */
	const CONFIG_FILE_EXT='.xml';
	/**
	 * @var TUrlMappingPattern[] list of patterns.
	 */
	protected $_patterns=array();
	/**
	 * @var TUrlMappingPattern matched pattern.
	 */
	private $_matched;
	/**
	 * @var string external configuration file
	 */
	private $_configFile=null;
	/**
	 * @var boolean whether to enable custom contructUrl
	 */
	private $_customUrl=false;
	/**
	 * @var array rules for constructing URLs
	 */
	protected $_constructRules=array();

	private $_urlPrefix='';

	private $_defaultMappingClass='TUrlMappingPattern';

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface.
	 * @param TXmlElement configuration for this module, can be null
	 * @throws TConfigurationException if module is configured in the global scope.
	 */
	public function init($xml)
	{
		parent::init($xml);
		if($this->getRequest()->getRequestResolved())
			throw new TConfigurationException('urlmapping_global_required');
		if($this->_configFile!==null)
			$this->loadConfigFile();
		$this->loadUrlMappings($xml);
		if($this->_urlPrefix==='')
			$this->_urlPrefix=$this->getRequest()->getApplicationUrl();
		$this->_urlPrefix=rtrim($this->_urlPrefix,'/');
	}

	/**
	 * Initialize the module from configuration file.
	 * @throws TConfigurationException if {@link getConfigFile ConfigFile} is invalid.
	 */
	protected function loadConfigFile()
	{
		if(is_file($this->_configFile))
 		{
			$dom=new TXmlDocument;
			$dom->loadFromFile($this->_configFile);
			$this->loadUrlMappings($dom);
		}
		else
			throw new TConfigurationException('urlmapping_configfile_inexistent',$this->_configFile);
	}

	/**
	 * Returns a value indicating whether to enable custom constructUrl.
	 * If true, constructUrl() will make use of the URL mapping rules to
	 * construct valid URLs.
	 * @return boolean whether to enable custom constructUrl. Defaults to false.
	 * @since 3.1.1
	 */
	public function getEnableCustomUrl()
	{
		return $this->_customUrl;
	}

	/**
	 * Sets a value indicating whether to enable custom constructUrl.
	 * If true, constructUrl() will make use of the URL mapping rules to
	 * construct valid URLs.
	 * @param boolean whether to enable custom constructUrl.
	 * @since 3.1.1
	 */
	public function setEnableCustomUrl($value)
	{
		$this->_customUrl=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return string the part that will be prefixed to the constructed URLs. Defaults to the requested script path (e.g. /path/to/index.php for a URL http://hostname/path/to/index.php)
	 * @since 3.1.1
	 */
	public function getUrlPrefix()
	{
		return $this->_urlPrefix;
	}

	/**
	 * @param string the part that will be prefixed to the constructed URLs. This is used by constructUrl() when EnableCustomUrl is set true.
	 * @see getUrlPrefix
	 * @since 3.1.1
	 */
	public function setUrlPrefix($value)
	{
		$this->_urlPrefix=$value;
	}

	/**
	 * @return string external configuration file. Defaults to null.
	 */
	public function getConfigFile()
	{
		return $this->_configFile;
	}

	/**
	 * @param string external configuration file in namespace format. The file
	 * must be suffixed with '.xml'.
	 * @throws TInvalidDataValueException if the file is invalid.
	 */
	public function setConfigFile($value)
	{
		if(($this->_configFile=Prado::getPathOfNamespace($value,self::CONFIG_FILE_EXT))===null)
			throw new TConfigurationException('urlmapping_configfile_invalid',$value);
	}

	/**
	 * @return string the default class of URL mapping patterns. Defaults to TUrlMappingPattern.
	 * @since 3.1.1
	 */
	public function getDefaultMappingClass()
	{
		return $this->_defaultMappingClass;
	}

	/**
	 * Sets the default class of URL mapping patterns.
	 * When a URL matching pattern does not specify "class" attribute, it will default to the class
	 * specified by this property. You may use either a class name or a namespace format of class (if the class needs to be included first.)
	 * @param string the default class of URL mapping patterns.
	 * @since 3.1.1
	 */
	public function setDefaultMappingClass($value)
	{
		$this->_defaultMappingClass=$value;
	}

	/**
	 * Load and configure each url mapping pattern.
	 * @param TXmlElement configuration node
	 * @throws TConfigurationException if specific pattern class is invalid
	 */
	protected function loadUrlMappings($xml)
	{
		foreach($xml->getElementsByTagName('url') as $url)
		{
			$properties=$url->getAttributes();
			if(($class=$properties->remove('class'))===null)
				$class=$this->getDefaultMappingClass();
			$pattern=Prado::createComponent($class,$this);
			if(!($pattern instanceof TUrlMappingPattern))
				throw new TConfigurationException('urlmapping_urlmappingpattern_required');
			foreach($properties as $name=>$value)
				$pattern->setSubproperty($name,$value);
			$this->_patterns[]=$pattern;
			$pattern->init($url);

			$key=$pattern->getServiceID().':'.$pattern->getServiceParameter();
			$this->_constructRules[$key][]=$pattern;
		}
	}

	/**
	 * Parses the request URL and returns an array of input parameters.
	 * This method overrides the parent implementation.
	 * The input parameters do not include GET and POST variables.
	 * This method uses the request URL path to find the first matching pattern. If found
	 * the matched pattern parameters are used to return as the input parameters.
	 * @return array list of input parameters
	 */
	public function parseUrl()
	{
		$request=$this->getRequest();
		foreach($this->_patterns as $pattern)
		{
			$matches=$pattern->getPatternMatches($request);
			if(count($matches)>0)
			{
				$this->_matched=$pattern;
				$params=array();
				foreach($matches as $key=>$value)
				{
					if(is_string($key))
						$params[$key]=$value;
				}
				if (!$pattern->getIsWildCardPattern())
					$params[$pattern->getServiceID()]=$pattern->getServiceParameter();
				return $params;
			}
		}
		return parent::parseUrl();
	}

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
	 * @since 3.1.1
	 */
	public function constructUrl($serviceID,$serviceParam,$getItems,$encodeAmpersand,$encodeGetItems)
	{
		if($this->_customUrl)
		{
	 		if(!(is_array($getItems) || ($getItems instanceof Traversable)))
	 			$getItems=array();
			$key=$serviceID.':'.$serviceParam;
			$wildCardKey = ($pos=strrpos($serviceParam,'.'))!==false ?
				$serviceID.':'.substr($serviceParam,0,$pos).'.*' : $serviceID.':*';
			if(isset($this->_constructRules[$key]))
			{
				foreach($this->_constructRules[$key] as $rule)
				{
					if($rule->supportCustomUrl($getItems))
						return $rule->constructUrl($getItems,$encodeAmpersand,$encodeGetItems);
				}
			} 
			elseif(isset($this->_constructRules[$wildCardKey]))
			{
				foreach($this->_constructRules[$wildCardKey] as $rule)
				{
					if($rule->supportCustomUrl($getItems))
					{
						$getItems['*']= $pos ? substr($serviceParam,$pos+1) : $serviceParam;
						return $rule->constructUrl($getItems,$encodeAmpersand,$encodeGetItems);
					}
				}
			}
		}
		return parent::constructUrl($serviceID,$serviceParam,$getItems,$encodeAmpersand,$encodeGetItems);
	}

	/**
	 * @return TUrlMappingPattern the matched pattern, null if not found.
	 */
	public function getMatchingPattern()
	{
		return $this->_matched;
	}
}

/**
 * TUrlMappingPattern class.
 *
 * TUrlMappingPattern represents a pattern used to parse and construct URLs.
 * If the currently requested URL matches the pattern, it will alter
 * the THttpRequest parameters. If a constructUrl() call matches the pattern
 * parameters, the pattern will generate a valid URL. In both case, only the PATH_INFO
 * part of a URL is parsed/constructed using the pattern.
 *
 * To specify the pattern, set the {@link setPattern Pattern} property.
 * {@link setPattern Pattern} takes a string expression with
 * parameter names enclosed between a left brace '{' and a right brace '}'.
 * The patterns for each parameter can be set using {@link getParameters Parameters}
 * attribute collection. For example
 * <code>
 * <url ... pattern="articles/{year}/{month}/{day}"
 *          parameters.year="\d{4}" parameters.month="\d{2}" parameters.day="\d+" />
 * </code>
 *
 * In the above example, the pattern contains 3 parameters named "year",
 * "month" and "day". The pattern for these parameters are, respectively,
 * "\d{4}" (4 digits), "\d{2}" (2 digits) and "\d+" (1 or more digits).
 * Essentially, the <tt>Parameters</tt> attribute name and values are used
 * as substrings in replacing the placeholders in the <tt>Pattern</tt> string
 * to form a complete regular expression string.
 *
 * For more complicated patterns, one may specify the pattern using a regular expression
 * by {@link setRegularExpression RegularExpression}. For example, the above pattern
 * is equivalent to the following regular expression-based pattern:
 * <code>
 * /^articles\/(?P<year>\d{4})\/(?P<month>\d{2})\/(?P<day>\d+)$/u
 * </code>
 * The above regular expression used the "named group" feature available in PHP.
 * Notice that you need to escape the slash in regular expressions.
 *
 * Thus, only an url that matches the pattern will be valid. For example,
 * a URL <tt>http://example.com/index.php/articles/2006/07/21</tt> will match the above pattern,
 * while <tt>http://example.com/index.php/articles/2006/07/hello</tt> will not
 * since the "day" parameter pattern is not satisfied.
 *
 * The parameter values are available through the <tt>THttpRequest</tt> instance (e.g.
 * <tt>$this->Request['year']</tt>).
 *
 * The {@link setServiceParameter ServiceParameter} and {@link setServiceID ServiceID}
 * (the default ID is 'page') set the service parameter and service id respectively.
 *
 * Since 3.1.4 you can also use simplyfied wildcard patterns to match multiple
 * ServiceParameters with a single rule. The pattern must contain the placeholder 
 * {*} for the ServiceParameter. For example
 *
 * <url ServiceParameter="adminpages.*" pattern="admin/{*}" />
 *
 * This rule will match an URL like <tt>http://example.com/index.php/admin/edituser</tt>
 * and resolve it to the page Application.pages.admin.edituser. The wildcard matching
 * is non-recursive. That means you have to add a rule for every subdirectory you
 * want to access pages in: 
 *
 * <url ServiceParameter="adminpages.users.*" pattern="useradmin/{*}" />
 *
 * It is still possible to define an explicit rule for a page in the wildcard path.
 * This rule has to preceed the wildcard rule.
 *
 * You can also use parameters with wildcard patterns. The parameters are then 
 * available with every matching page:
 *
 * <url ServiceParameter="adminpages.*" pattern="admin/{*}/{id}" parameters.id="\d+" />
 *
 * To enable automatic parameter encoding in a path format fro wildcard patterns you can set 
 * {@setUrlFormat UrlFormat} to 'Path': 
 *
 * <url ServiceParameter="adminpages.*" pattern="admin/{*}" UrlFormat="Path" />
 *
 * This will create and parse URLs of the form 
 * <tt>.../index.php/admin/listuser/param1/value1/param2/value2</tt>.
 *
 * Use {@setUrlParamSeparator} to define another separator character between parameter
 * name and value. Parameter/value pairs are always separated by a '/'.
 *
 * <url ServiceParameter="adminpages.*" pattern="admin/{*}" UrlFormat="Path" UrlParamSeparator="-" />
 *
 * <tt>.../index.php/admin/listuser/param1-value1/param2-value2</tt>.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TUrlMapping.php 2918 2011-05-21 17:10:29Z ctrlaltca@gmail.com $
 * @package System.Web
 * @since 3.0.5
 */
class TUrlMappingPattern extends TComponent
{
	/**
	 * @var string service parameter such as Page class name.
	 */
	private $_serviceParameter;
	/**
	 * @var string service ID, default is 'page'.
	 */
	private $_serviceID='page';
	/**
	 * @var string url pattern to match.
	 */
	private $_pattern;
	/**
	 * @var TMap parameter regular expressions.
	 */
	private $_parameters;
	/**
	 * @var string regular expression pattern.
	 */
	private $_regexp='';

	private $_customUrl=true;

	private $_manager;

	private $_caseSensitive=true;

	private $_isWildCardPattern=false;

	private $_urlFormat=THttpRequestUrlFormat::Get;

	private $_separator='/';

	/**
	 * Constructor.
	 * @param TUrlManager the URL manager instance
	 */
	public function __construct(TUrlManager $manager)
	{
		$this->_manager=$manager;
		$this->_parameters=new TAttributeCollection;
		$this->_parameters->setCaseSensitive(true);
	}

	/**
	 * @return TUrlManager the URL manager instance
	 */
	public function getManager()
	{
		return $this->_manager;
	}

	/**
	 * Initializes the pattern.
	 * @param TXmlElement configuration for this module.
	 * @throws TConfigurationException if service parameter is not specified
	 */
	public function init($config)
	{
		if($this->_serviceParameter===null)
			throw new TConfigurationException('urlmappingpattern_serviceparameter_required', $this->getPattern());
		if(strpos($this->_serviceParameter,'*')!==false) 
		    $this->_isWildCardPattern=true;
	}

	/**
	 * Substitute the parameter key value pairs as named groupings
	 * in the regular expression matching pattern.
	 * @return string regular expression pattern with parameter subsitution
	 */
	protected function getParameterizedPattern()
	{
		$params=array();
		$values=array();
		foreach($this->_parameters as $key=>$value)
		{
			$params[]='{'.$key.'}';
			$values[]='(?P<'.$key.'>'.$value.')';
		}
		if ($this->getIsWildCardPattern()) {
		    $params[]='{*}';
		    // service parameter must not contain '=' and '/'
		    $values[]='(?P<'.$this->getServiceID().'>[^=/]+)';
		}
		$params[]='/';
		$values[]='\\/';
		$regexp=str_replace($params,$values,trim($this->getPattern(),'/').'/');
		if ($this->_urlFormat===THttpRequestUrlFormat::Get)
		    $regexp='/^'.$regexp.'$/u';
		else 
		    $regexp='/^'.$regexp.'(?P<urlparams>.*)$/u';

		if(!$this->getCaseSensitive())
			$regexp.='i';
		return $regexp;
	}

	/**
	 * @return string full regular expression mapping pattern
	 */
	public function getRegularExpression()
	{
		return $this->_regexp;
	}

	/**
	 * @param string full regular expression mapping pattern.
	 */
	public function setRegularExpression($value)
	{
		$this->_regexp=$value;
	}

	/**
	 * @return boolean whether the {@link getPattern Pattern} should be treated as case sensititve. Defaults to true.
	 */
	public function getCaseSensitive()
	{
		return $this->_caseSensitive;
	}

	/**
	 * @param boolean whether the {@link getPattern Pattern} should be treated as case sensititve.
	 */
	public function setCaseSensitive($value)
	{
		$this->_caseSensitive=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @param string service parameter, such as page class name.
	 */
	public function setServiceParameter($value)
	{
		$this->_serviceParameter=$value;
	}

	/**
	 * @return string service parameter, such as page class name.
	 */
	public function getServiceParameter()
	{
		return $this->_serviceParameter;
	}

	/**
	 * @param string service id to handle.
	 */
	public function setServiceID($value)
	{
		$this->_serviceID=$value;
	}

	/**
	 * @return string service id.
	 */
	public function getServiceID()
	{
		return $this->_serviceID;
	}

	/**
	 * @return string url pattern to match. Defaults to ''.
	 */
	public function getPattern()
	{
		return $this->_pattern;
	}

	/**
	 * @param string url pattern to match.
	 */
	public function setPattern($value)
	{
		$this->_pattern = $value;
	}

	/**
	 * @return TAttributeCollection parameter key value pairs.
	 */
	public function getParameters()
	{
		return $this->_parameters;
	}

	/**
	 * @param TAttributeCollection new parameter key value pairs.
	 */
	public function setParameters($value)
	{
		$this->_parameters=$value;
	}

	/**
	 * Uses URL pattern (or full regular expression if available) to
	 * match the given url path.
	 * @param THttpRequest the request module
	 * @return array matched parameters, empty if no matches.
	 */
	public function getPatternMatches($request)
	{
		$matches=array();
		if(($pattern=$this->getRegularExpression())!=='')
			preg_match($pattern,$request->getPathInfo(),$matches);
		else
			preg_match($this->getParameterizedPattern(),trim($request->getPathInfo(),'/').'/',$matches);

		if($this->getIsWildCardPattern() && isset($matches[$this->_serviceID]))
			$matches[$this->_serviceID]=str_replace('*',$matches[$this->_serviceID],$this->_serviceParameter);

		if (isset($matches['urlparams']))
		{
			$params=explode('/',$matches['urlparams']);
			if ($this->_separator==='/') 
			{
				while($key=array_shift($params))
					$matches[$key]=($value=array_shift($params)) ? $value : '';
			} 
			else 
			{
				array_pop($params);
				foreach($params as $param)
				{
					list($key,$value)=explode($this->_separator,$param,2);
					$matches[$key]=$value;
				}
			}
			unset($matches['urlparams']);
		}

		return $matches;
	}

	/**
	 * Returns a value indicating whether to use this pattern to construct URL.
	 * @return boolean whether to enable custom constructUrl. Defaults to true.
	 * @since 3.1.1
	 */
	public function getEnableCustomUrl()
	{
		return $this->_customUrl;
	}

	/**
	 * Sets a value indicating whether to enable custom constructUrl using this pattern
	 * @param boolean whether to enable custom constructUrl.
	 */
	public function setEnableCustomUrl($value)
	{
		$this->_customUrl=TPropertyValue::ensureBoolean($value);
	}

	/**
	 * @return boolean whether this pattern is a wildcard pattern
	 * @since 3.1.4
	 */
	public function getIsWildCardPattern() {
		return $this->_isWildCardPattern;
	}

	/**
	 * @return THttpRequestUrlFormat the format of URLs. Defaults to THttpRequestUrlFormat::Get.
	 */
	public function getUrlFormat()
	{
		return $this->_urlFormat;
	}

	/**
	 * Sets the format of URLs constructed and interpreted by this pattern.
	 * A Get URL format is like index.php?name1=value1&name2=value2
	 * while a Path URL format is like index.php/name1/value1/name2/value.
	 * The separating character between name and value can be configured with 
	 * {@link setUrlParamSeparator} and defaults to '/'.
	 * Changing the UrlFormat will affect {@link constructUrl} and how GET variables
	 * are parsed.
	 * @param THttpRequestUrlFormat the format of URLs.
	 * @param since 3.1.4
	 */
	public function setUrlFormat($value)
	{
		$this->_urlFormat=TPropertyValue::ensureEnum($value,'THttpRequestUrlFormat');
	}

	/**
	 * @return string separator used to separate GET variable name and value when URL format is Path. Defaults to slash '/'.
	 */
	public function getUrlParamSeparator()
	{
		return $this->_separator;
	}

	/**
	 * @param string separator used to separate GET variable name and value when URL format is Path.
	 * @throws TInvalidDataValueException if the separator is not a single character
	 */
	public function setUrlParamSeparator($value)
	{
		if(strlen($value)===1)
			$this->_separator=$value;
		else
			throw new TInvalidDataValueException('httprequest_separator_invalid');
	}

	/**
	 * @param array list of GET items to be put in the constructed URL
	 * @return boolean whether this pattern IS the one for constructing the URL with the specified GET items.
	 * @since 3.1.1
	 */
	public function supportCustomUrl($getItems)
	{
		if(!$this->_customUrl || $this->getPattern()===null)
			return false;
		foreach($this->_parameters as $key=>$value)
		{
			if(!isset($getItems[$key]))
				return false;
		}
		return true;
	}

	/**
	 * Constructs a URL using this pattern.
	 * @param array list of GET variables
	 * @param boolean whether the ampersand should be encoded in the constructed URL
	 * @param boolean whether the GET variables should be encoded in the constructed URL
	 * @return string the constructed URL
	 * @since 3.1.1
	 */
	public function constructUrl($getItems,$encodeAmpersand,$encodeGetItems)
	{
		$extra=array();
		$replace=array();
		// for the GET variables matching the pattern, put them in the URL path
		foreach($getItems as $key=>$value)
		{
			if($this->_parameters->contains($key) || $key==='*' && $this->getIsWildCardPattern())
				$replace['{'.$key.'}']=$encodeGetItems ? rawurlencode($value) : $value;
			else
				$extra[$key]=$value;
		}

		$url=$this->_manager->getUrlPrefix().'/'.ltrim(strtr($this->getPattern(),$replace),'/');

		// for the rest of the GET variables, put them in the query string
		if(count($extra)>0)
		{
			if ($this->_urlFormat===THttpRequestUrlFormat::Path && $this->getIsWildCardPattern()) {
				foreach ($extra as $name=>$value)
					$url.='/'.$name.$this->_separator.($encodeGetItems?rawurlencode($value):$value);
				return $url;
			}

			$url2='';
			$amp=$encodeAmpersand?'&amp;':'&';
			if($encodeGetItems)
			{
				foreach($extra as $name=>$value)
				{
					if(is_array($value))
					{
						$name=rawurlencode($name.'[]');
						foreach($value as $v)
							$url2.=$amp.$name.'='.rawurlencode($v);
					}
					else
						$url2.=$amp.rawurlencode($name).'='.rawurlencode($value);
				}
			}
			else
			{
				foreach($extra as $name=>$value)
				{
					if(is_array($value))
					{
						foreach($value as $v)
							$url2.=$amp.$name.'[]='.$v;
					}
					else
						$url2.=$amp.$name.'='.$value;
				}
			}
			$url=$url.'?'.substr($url2,strlen($amp));
		}
		return $url;
	}
}

