<?php
/**
 * THttpResponseAdatper class
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id$
 * @package System.Web
 */

/**
 * THttpResponseAdapter class.
 *
 * THttpResponseAdapter allows the base http response class to change behavior
 * without change the class hierarchy.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id$
 * @package System.Web
 * @since 3.0
 */
class THttpResponseAdapter extends TApplicationComponent
{
	/**
	 * @var THttpResponse the response object the adapter is attached.
	 */
	private $_response;

	/**
	 * Constructor. Attach a response to be adapted.
	 * @param THttpResponse the response object the adapter is to attach to.
	 */
	public function __construct($response)
	{
		$this->_response=$response;
	}

	/**
	 * @return THttpResponse the response object adapted.
	 */
	public function getResponse()
	{
		return $this->_response;
	}

	/**
	 * This method is invoked when the response flushes the content and headers.
	 * Default implementation calls the attached response flushContent method.
	 */
	public function flushContent()
	{
		$this->_response->flushContent();
	}

	/**
	 * This method is invoked when the response is to redirect to another page.
	 * @param string new url to redirect to.
	 */
	public function httpRedirect($url)
	{
		$this->_response->httpRedirect($url);
	}

	/**
	 * This method is invoked when a new HtmlWriter needs to be created.
	 * Default implementation calls the attached response createNewHtmlWriter method.
	 * @param string type of the HTML writer to be created.
	 * @param ITextWriter the writer responsible for holding the content.
	 */
	public function createNewHtmlWriter($type, $writer)
	{
		return $this->_response->createNewHtmlWriter($type,$writer);
	}
}

?>
