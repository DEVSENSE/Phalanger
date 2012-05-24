<?php
/**
 * TCallbackEventParameter class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Web.UI.ActiveControls
 */

/**
 * TCallbackEventParameter class.
 *
 * The TCallbackEventParameter provides the parameter passed during the callback
 * request in the {@link getCallbackParameter CallbackParameter} property. The
 * callback response content (e.g. new HTML content) must be rendered
 * using an THtmlWriter obtained from the {@link getNewWriter NewWriter}
 * property, which returns a <b>NEW</b> instance of TCallbackResponseWriter.
 *
 * Each instance TCallbackResponseWriter is associated with a unique
 * boundary delimited. By default each panel only renders its own content.
 * To replace the content of ONE panel with that of rendered from multiple panels
 * use the same writer instance for the panels to be rendered.
 *
 * The response data (i.e., passing results back to the client-side
 * callback handler function) can be set using {@link setResponseData ResponseData} property.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @version $Id: TActivePageAdapter.php 1648 2007-01-24 05:52:22Z wei $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TCallbackEventParameter extends TEventParameter
{
	/**
	 * @var THttpResponse output content.
	 */
	private $_response;
	/**
	 * @var mixed callback request parameter.
	 */
	private $_parameter;

	/**
	 * Creates a new TCallbackEventParameter.
	 */
	public function __construct($response, $parameter)
	{
		$this->_response = $response;
		$this->_parameter = $parameter;
	}

	/**
	 * @return TCallbackResponseWriter holds the response content.
	 */
	public function getNewWriter()
	{
		return $this->_response->createHtmlWriter(null);
	}

	/**
	 * @return mixed callback request parameter.
	 */
	public function getCallbackParameter()
	{
		return $this->_parameter;
	}

	/**
	 * @param mixed callback response data.
	 */
	public function setResponseData($value)
	{
		$this->_response->getAdapter()->setResponseData($value);
	}

	/**
	 * @return mixed callback response data.
	 */
	public function getResponseData()
	{
		return $this->_response->getAdapter()->getResponseData();
	}
}

