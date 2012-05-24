<?php
/**
 * TActivePageAdapter, TCallbackErrorHandler and TInvalidCallbackException class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActivePageAdapter.php 2681 2009-06-27 07:10:41Z godzilla80@gmx.net $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load callback response adapter class.
 */
Prado::using('System.Web.UI.ActiveControls.TCallbackResponseAdapter');
Prado::using('System.Web.UI.ActiveControls.TCallbackClientScript');
Prado::using('System.Web.UI.ActiveControls.TCallbackEventParameter');

/**
 * TActivePageAdapter class.
 *
 * Callback request handler.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @version $Id: TActivePageAdapter.php 2681 2009-06-27 07:10:41Z godzilla80@gmx.net $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActivePageAdapter extends TControlAdapter
{
	/**
	 * Callback response data header name.
	 */
	const CALLBACK_DATA_HEADER = 'X-PRADO-DATA';
	/**
	 * Callback response client-side action header name.
	 */
	const CALLBACK_ACTION_HEADER = 'X-PRADO-ACTIONS';
	/**
	 * Callback error header name.
	 */
	const CALLBACK_ERROR_HEADER = 'X-PRADO-ERROR';
	/**
	 * Callback page state header name.
	 */
	const CALLBACK_PAGESTATE_HEADER = 'X-PRADO-PAGESTATE';

	/**
	 * Callback redirect url header name.
	 */
	const CALLBACK_REDIRECT = 'X-PRADO-REDIRECT';

	/**
	 * @var ICallbackEventHandler callback event handler.
	 */
	private $_callbackEventTarget;
	/**
	 * @var mixed callback event parameter.
	 */
	private $_callbackEventParameter;
	/**
	 * @var TCallbackClientScript callback client script handler
	 */
	private $_callbackClient;

	private $_controlsToRender=array();

	/**
	 * Constructor, trap errors and exception to let the callback response
	 * handle them.
	 */
	public function __construct(TPage $control)
	{
		parent::__construct($control);

		//TODO: can this be done later?
		$response = $this->getApplication()->getResponse();
		$response->setAdapter(new TCallbackResponseAdapter($response));

		$this->trapCallbackErrorsExceptions();
	}

	/**
	 * Process the callback request.
	 * @param THtmlWriter html content writer.
	 */
	public function processCallbackEvent($writer)
	{
		Prado::trace("ActivePage raiseCallbackEvent()",'System.Web.UI.ActiveControls.TActivePageAdapter');
		$this->raiseCallbackEvent();
	}

	/**
	 * Register a control for defered render() call.
	 * @param TControl control for defered rendering
	 * @param THtmlWriter the renderer
	 */
	public function registerControlToRender($control,$writer)
	{
		$id = $control->getUniqueID();
		if(!isset($this->_controlsToRender[$id]))
			$this->_controlsToRender[$id] = array($control,$writer);
	}

	/**
	 * Trap errors and exceptions to be handled by TCallbackErrorHandler.
	 */
	protected function trapCallbackErrorsExceptions()
	{
		$this->getApplication()->setErrorHandler(new TCallbackErrorHandler);
	}

	/**
	 * Render the callback response.
	 * @param THtmlWriter html content writer.
	 */
	public function renderCallbackResponse($writer)
	{
		Prado::trace("ActivePage renderCallbackResponse()",'System.Web.UI.ActiveControls.TActivePageAdapter');
		if(($url = $this->getResponse()->getAdapter()->getRedirectedUrl())===null)
			$this->renderResponse($writer);
		else
			$this->redirect($url);
	}

	/**
	 * Redirect url on the client-side using javascript.
	 * @param string new url to load.
	 */
	protected function redirect($url)
	{
		Prado::trace("ActivePage redirect()",'System.Web.UI.ActiveControls.TActivePageAdapter');
		$this->appendContentPart($this->getResponse(), self::CALLBACK_REDIRECT, $url);
		//$this->getResponse()->appendHeader(self::CALLBACK_REDIRECT.': '.$url);
	}

	/**
	 * Renders the callback response by adding additional callback data and
	 * javascript actions in the header and page state if required.
	 * @param THtmlWriter html content writer.
	 */
	protected function renderResponse($writer)
	{
		Prado::trace("ActivePage renderResponse()",'System.Web.UI.ActiveControls.TActivePageAdapter');
		//renders all the defered render() calls.
		foreach($this->_controlsToRender as $rid => $forRender)
			$forRender[0]->render($forRender[1]);

		$response = $this->getResponse();

		//send response data in header
		if($response->getHasAdapter())
		{
			$responseData = $response->getAdapter()->getResponseData();
			if($responseData!==null)
			{
				$data = TJavaScript::jsonEncode($responseData);

				$this->appendContentPart($response, self::CALLBACK_DATA_HEADER, $data);
				//$response->appendHeader(self::CALLBACK_DATA_HEADER.': '.$data);
			}
		}

		//sends page state in header
		if(($handler = $this->getCallbackEventTarget()) !== null)
		{
			if($handler->getActiveControl()->getClientSide()->getEnablePageStateUpdate())
			{
				$pagestate = $this->getPage()->getClientState();
				$this->appendContentPart($response, self::CALLBACK_PAGESTATE_HEADER, $pagestate);
				//$response->appendHeader(self::CALLBACK_PAGESTATE_HEADER.': '.$pagestate);
			}
		}

		//safari must receive at least 1 byte of data.
		$writer->write(" ");

		//output the end javascript
		if($this->getPage()->getClientScript()->hasEndScripts())
		{
			$writer = $response->createHtmlWriter();
			$this->getPage()->getClientScript()->renderEndScripts($writer);
			$this->getPage()->getCallbackClient()->evaluateScript($writer);
		}

		//output the actions
		$executeJavascript = $this->getCallbackClientHandler()->getClientFunctionsToExecute();
		$actions = TJavaScript::jsonEncode($executeJavascript);
		$this->appendContentPart($response, self::CALLBACK_ACTION_HEADER, $actions);
		//$response->appendHeader(self::CALLBACK_ACTION_HEADER.': '.$actions);
	}

	/**
	 * Appends data or javascript code to the body content surrounded with delimiters
	 */
	private function appendContentPart($response, $delimiter, $data)
	{
		$content = $response->createHtmlWriter();
		$content->getWriter()->setBoundary($delimiter);
		$content->write($data);
	}

	/**
	 * Trys to find the callback event handler and raise its callback event.
	 * @throws TInvalidCallbackException if call back target is not found.
	 * @throws TInvalidCallbackException if the requested target does not
	 * implement ICallbackEventHandler.
	 */
	private function raiseCallbackEvent()
	{
		 if(($callbackHandler=$this->getCallbackEventTarget())!==null)
		 {
			if($callbackHandler instanceof ICallbackEventHandler)
			{
				$param = $this->getCallbackEventParameter();
				$result = new TCallbackEventParameter($this->getResponse(), $param);
				$callbackHandler->raiseCallbackEvent($result);
			}
			else
			{
				throw new TInvalidCallbackException(
					'callback_invalid_handler', $callbackHandler->getUniqueID());
			}
		 }
		 else
		 {
		 	$target = $this->getRequest()->itemAt(TPage::FIELD_CALLBACK_TARGET);
		 	throw new TInvalidCallbackException('callback_invalid_target', $target);
		 }
	}

	/**
	 * @return TControl the control responsible for the current callback event,
	 * null if nonexistent
	 */
	public function getCallbackEventTarget()
	{
		if($this->_callbackEventTarget===null)
		{
			$eventTarget=$this->getRequest()->itemAt(TPage::FIELD_CALLBACK_TARGET);
			if(!empty($eventTarget))
				$this->_callbackEventTarget=$this->getPage()->findControl($eventTarget);
		}
		return $this->_callbackEventTarget;
	}

	/**
	 * Registers a control to raise callback event in the current request.
	 * @param TControl control registered to raise callback event.
	 */
	public function setCallbackEventTarget(TControl $control)
	{
		$this->_callbackEventTarget=$control;
	}

	/**
	 * Gets callback parameter. JSON encoding is assumed.
	 * @return string postback event parameter
	 */
	public function getCallbackEventParameter()
	{
		if($this->_callbackEventParameter===null)
		{
			$param = $this->getRequest()->itemAt(TPage::FIELD_CALLBACK_PARAMETER);
			if(strlen($param) > 0)
				$this->_callbackEventParameter=TJavaScript::jsonDecode((string)$param);
		}
		return $this->_callbackEventParameter;
	}

	/**
	 * @param mixed postback event parameter
	 */
	public function setCallbackEventParameter($value)
	{
		$this->_callbackEventParameter=$value;
	}

	/**
	 * Gets the callback client script handler. It handlers the javascript functions
	 * to be executed during the callback response.
	 * @return TCallbackClientScript callback client handler.
	 */
	public function getCallbackClientHandler()
	{
		if($this->_callbackClient===null)
			$this->_callbackClient = new TCallbackClientScript;
		return $this->_callbackClient;
	}
}

/**
 * TCallbackErrorHandler class.
 *
 * Captures errors and exceptions and send them back during callback response.
 * When the application is in debug mode, the error and exception stack trace
 * are shown. A TJavascriptLogger must be present on the client-side to view
 * the error stack trace.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActivePageAdapter.php 2681 2009-06-27 07:10:41Z godzilla80@gmx.net $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TCallbackErrorHandler extends TErrorHandler
{
	/**
	 * Displays the exceptions to the client-side TJavascriptLogger.
	 * A HTTP 500 status code is sent and the stack trace is sent as JSON encoded.
	 * @param Exception exception details.
	 */
	protected function displayException($exception)
	{
		if($this->getApplication()->getMode()===TApplication::STATE_DEBUG)
		{
			$response = $this->getApplication()->getResponse();
			$trace = TJavaScript::jsonEncode($this->getExceptionStackTrace($exception));
			$response->setStatusCode(500, 'Internal Server Error');
			$response->appendHeader(TActivePageAdapter::CALLBACK_ERROR_HEADER.': '.$trace);
		}
		else
		{
			error_log("Error happened while processing an existing error:\n".$exception->__toString());
			header('HTTP/1.0 500 Internal Server Error', true, 500);
		}
		$this->getApplication()->getResponse()->flush();
	}

	/**
	 * @param Exception exception details.
	 * @return array exception stack trace details.
	 */
	private function getExceptionStackTrace($exception)
	{
		$data['code']=$exception->getCode() > 0 ? $exception->getCode() : 500;
		$data['file']=$exception->getFile();
		$data['line']=$exception->getLine();
		$data['trace']=$exception->getTrace();
		if($exception instanceof TPhpErrorException)
		{
			// if PHP exception, we want to show the 2nd stack level context
			// because the 1st stack level is of little use (it's in error handler)
			if(isset($trace[0]) && isset($trace[0]['file']) && isset($trace[0]['line']))
			{
				$data['file']=$trace[0]['file'];
				$data['line']=$trace[0]['line'];
			}
		}
		$data['type']=get_class($exception);
		$data['message']=$exception->getMessage();
		$data['version']=$_SERVER['SERVER_SOFTWARE'].' '.Prado::getVersion();
		$data['time']=@strftime('%Y-%m-%d %H:%M',time());
		return $data;
	}
}

/**
 * TInvalidCallbackException class.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActivePageAdapter.php 2681 2009-06-27 07:10:41Z godzilla80@gmx.net $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TInvalidCallbackException extends TException
{
}

