<?php
/**
 * TValueTriggeredCallback class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TValueTriggeredCallback.php 2960 2011-06-02 17:44:51Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 */

Prado::using('System.Web.UI.ActiveControls.TTriggeredCallback');

/**
 * TValueTriggeredCallback Class
 *
 * Observes the value with {@link setPropertyName PropertyName} of a
 * control with {@link setControlID ControlID}. Changes to the observed
 * property value will trigger a new callback request. The property value is checked
 * for changes every{@link setInterval Interval} seconds.
 *
 * A {@link setDecayRate DecayRate} can be set to increase the polling
 * interval linearly if no changes are observed. Once a change is
 * observed, the polling interval is reset to the original value.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TValueTriggeredCallback.php 2960 2011-06-02 17:44:51Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TValueTriggeredCallback extends TTriggeredCallback
{
	/**
	 * @return string The control property name to observe value changes.
	 */
	public function getPropertyName()
	{
		return $this->getViewState('PropertyName', '');
	}

	/**
	 * Sets the control property name to observe value changes that fires the callback request.
	 * @param string The control property name to observe value changes.
	 */
	public function setPropertyName($value)
	{
		$this->setViewState('PropertyName', $value, '');
	}

	/**
	 * Sets the polling interval in seconds to observe property changes.
	 * Default is 1 second.
	 * @param float polling interval in seconds.
	 */
	public function setInterval($value)
	{
		$this->setViewState('Interval', TPropertyValue::ensureFloat($value), 1);
	}

	/**
	 * @return float polling interval, 1 second default.
	 */
	public function getInterval()
	{
		return $this->getViewState('Interval', 1);
	}

	/**
	 * Gets the decay rate between callbacks. Default is 0;
	 * @return float decay rate between callbacks.
	 */
	public function getDecayRate()
	{
		return $this->getViewState('Decay', 0);
	}

	/**
	 * Sets the decay rate between callback. Default is 0;
	 * @param float decay rate between callbacks.
	 */
	public function setDecayRate($value)
	{
		$decay = TPropertyValue::ensureFloat($value);
		if($decay < 0)
			throw new TConfigurationException('callback_decay_be_not_negative', $this->getID());
		$this->setViewState('Decay', $decay);
	}

	/**
	 * @return array list of timer options for client-side.
	 */
	protected function getTriggerOptions()
	{
		$options = parent::getTriggerOptions();
		$options['PropertyName'] = $this->getPropertyName();
		$options['Interval'] = $this->getInterval();
		$options['Decay'] = $this->getDecayRate();
		return $options;
	}

	/**
	 * Registers the javascript code for initializing the active control.
	 * @param THtmlWriter the renderer.
	 */
	public function render($writer)
	{
		parent::render($writer);
		$this->getActiveControl()->registerCallbackClientScript(
			$this->getClientClassName(), $this->getTriggerOptions());
	}

	/**
	 * @return string corresponding javascript class name for TEventTriggeredCallback.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TValueTriggeredCallback';
	}
}
