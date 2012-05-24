<?php
/**
 * TActiveCustomValidator class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveCustomValidator.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.ActiveControls
 */

Prado::using('System.Web.UI.ActiveControls.TCallbackClientSide');

/**
 * TActiveCustomValidator Class
 *
 * Performs custom validation using only server-side {@link onServerValidate onServerValidate}
 * validation event. The client-side uses callbacks to raise
 * the {@link onServerValidate onServerValidate} event.
 *
 * Beware that the {@link onServerValidate onServerValidate} may be
 * raised when the control to validate on the client side
 * changes value, that is, the server validation may be called many times.
 *
 * After the callback or postback, the {@link onServerValidate onServerValidate}
 * is raised once more. The {@link getIsCallback IsCallback} property
 * will be true when validation is made during a callback request.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveCustomValidator.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveCustomValidator extends TCustomValidator
	implements ICallbackEventHandler, IActiveControl
{
	/**
	 * @var boolean true if validation is made during a callback request.
	 */
	private $_isCallback = false;

	/**
	 * @return boolean true if validation is made during a callback request.
	 */
	public function getIsCallback()
	{
		return $this->_isCallback;
	}

	/**
	 * Creates a new callback control, sets the adapter to
	 * TActiveControlAdapter. If you override this class, be sure to set the
	 * adapter appropriately by, for example, by calling this constructor.
	 */
	public function __construct()
	{
		parent::__construct();
		$this->setAdapter(new TActiveControlAdapter($this));
		$this->getActiveControl()->setClientSide(new TActiveCustomValidatorClientSide);
	}

	/**
	 * @return TBaseActiveCallbackControl standard callback control options.
	 */
	public function getActiveControl()
	{
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * @return TCallbackClientSide client side request options.
	 */
	public function getClientSide()
	{
		return $this->getAdapter()->getBaseActiveControl()->getClientSide();
	}

	/**
	 * Client validation function is NOT supported.
	 */
	public function setClientValidationFunction($value)
	{
		throw new TNotSupportedException('tactivecustomvalidator_clientfunction_unsupported',
			get_class($this));
	}

	/**
	 * Raises the callback event. This method is required by {@link
	 * ICallbackEventHandler} interface. The {@link onServerValidate
	 * OnServerValidate} event is raised first and then the
	 * {@link onCallback OnCallback} event.
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the event parameter
	 */
 	public function raiseCallbackEvent($param)
	{
		$this->_isCallback = true;
		$result = $this->onServerValidate($param->getCallbackParameter());
		$param->setResponseData($result);
		$this->onCallback($param);
	}

	/**
	 * This method is invoked when a callback is requested. The method raises
	 * 'OnCallback' event to fire up the event handlers. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onCallback($param)
	{
		$this->raiseEvent('OnCallback', $this, $param);
	}

	/**
	 * Returns an array of javascript validator options.
	 * @return array javascript validator options.
	 */
	protected function getClientScriptOptions()
	{
		$options=TBaseValidator::getClientScriptOptions();
		$options['EventTarget'] = $this->getUniqueID();
		return $options;
	}

	/**
	 * Sets the text for the error message. Updates client-side erorr message.
	 * @param string the error message
	 */
	public function setErrorMessage($value)
	{
		parent::setErrorMessage($value);
		if($this->getActiveControl()->canUpdateClientSide())
		{
			$client = $this->getPage()->getCallbackClient();
			$func = 'Prado.Validation.setErrorMessage';
			$client->callClientFunction($func, array($this, $value));
		}
	}

		/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control.
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		TBaseValidator::registerClientScriptValidator();
	}

	/**
	 * @return string corresponding javascript class name for this this.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TActiveCustomValidator';
	}
}

/**
 * Custom Validator callback client side options class.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveCustomValidator.php 2624 2009-03-19 21:20:47Z godzilla80@gmx.net $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveCustomValidatorClientSide extends TCallbackClientSide
{
	/**
	 * @return string javascript code for client-side OnValidate event.
	 */
	public function getOnValidate()
	{
		return $this->getOption('OnValidate');
	}

	/**
	 * Client-side OnValidate validator event is raise before the validators
	 * validation functions are called.
	 * @param string javascript code for client-side OnValidate event.
	 */
	public function setOnValidate($javascript)
	{
		$this->setFunction('OnValidate', $javascript);
	}

	/**
	 * Client-side OnSuccess event is raise after validation is successfull.
	 * This will override the default client-side validator behaviour.
	 * @param string javascript code for client-side OnSuccess event.
	 */
	public function setOnValidationSuccess($javascript)
	{
		$this->setFunction('OnValidationSuccess', $javascript);
	}

	/**
	 * @return string javascript code for client-side OnSuccess event.
	 */
	public function getOnValidationSuccess()
	{
		return $this->getOption('OnValidationSuccess');
	}

	/**
	 * Client-side OnError event is raised after validation failure.
	 * This will override the default client-side validator behaviour.
	 * @param string javascript code for client-side OnError event.
	 */
	public function setOnValidationError($javascript)
	{
		$this->setFunction('OnValidationError', $javascript);
	}

	/**
	 * @return string javascript code for client-side OnError event.
	 */
	public function getOnValidationError()
	{
		return $this->getOption('OnValidationError');
	}

	/**
	 * @param boolean true to revalidate when the control to validate changes value.
	 */
	public function setObserveChanges($value)
	{
		$this->setOption('ObserveChanges', TPropertyValue::ensureBoolean($value));
	}

	/**
	 * @return boolean true to observe changes.
	 */
	public function getObserveChanges()
	{
		$changes = $this->getOption('ObserveChanges');
		return ($changes===null) ? true : $changes;
	}
}
