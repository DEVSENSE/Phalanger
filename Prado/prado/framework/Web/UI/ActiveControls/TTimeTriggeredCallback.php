<?php
/**
 * TTimeTriggeredCallback class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTimeTriggeredCallback.php 2532 2008-10-16 09:12:20Z tof $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active callback control.
 */
Prado::using('System.Web.UI.ActiveControls.TCallback');

/**
 * TTimeTriggeredCallback class.
 *
 * TTimeTriggeredCallback sends callback request every {@link setInterval Interval} seconds.
 * Upon each callback request, the {@link onCallback OnCallback} event is raised.
 *
 * The timer can be started by calling {@link startTimer()} and stopped using
 * {@link stopTimer()}. The timer can be automatically started when
 * {@link setStartTimerOnLoad StartTimerOnLoad} is true.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TTimeTriggeredCallback.php 2532 2008-10-16 09:12:20Z tof $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TTimeTriggeredCallback extends TCallback
{
	/**
	 * @return float seconds between callback requests. Default is 1 second.
	 */
	public function getInterval()
	{
		return $this->getViewState('Interval', 1);
	}

	/**
	 * @param float seconds between callback requests, must be a positive number, default is 1 second.
	 */
	public function setInterval($value)
	{
		$interval = TPropertyValue::ensureFloat($value);
		if($interval <= 0)
			throw new TConfigurationException('callback_interval_be_positive', $this->getID());
		$this->setViewState('Interval', $interval, 1);
		if ($this->getActiveControl()->canUpdateClientSide()){
			$client = $this->getPage()->getCallbackClient();
			$client->callClientFunction('Prado.WebUI.TTimeTriggeredCallback.setInterval', array($this, $interval));
		}
	}

	/**
	 * Registers the javascript code to start the timer.
	 */
	public function startTimer()
	{
		$client = $this->getPage()->getCallbackClient();
		$client->callClientFunction('Prado.WebUI.TTimeTriggeredCallback.start', array($this));
	}

	/**
	 * Registers the javascript code to stop the timer.
	 */
	public function stopTimer()
	{
		$client = $this->getPage()->getCallbackClient();
		$client->callClientFunction('Prado.WebUI.TTimeTriggeredCallback.stop', array($this));
	}

	/**
	 * @param boolean true to start the timer when page loads.
	 */
	public function setStartTimerOnLoad($value)
	{
		$this->setViewState('StartTimerOnLoad',
				TPropertyValue::ensureBoolean($value), false);
	}

	/**
	 * @return boolean true to start the timer when page loads.
	 */
	public function getStartTimerOnLoad()
	{
		return $this->getViewState('StartTimerOnLoad', false);
	}

	/**
	 * @return array list of timer options for client-side.
	 */
	protected function getTriggerOptions()
	{
		$options['ID'] = $this->getClientID();
		$options['EventTarget']= $this->getUniqueID();
		$options['Interval'] = $this->getInterval();
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
		if($this->getStartTimerOnLoad()){
			$id = $this->getClientID();
			$code = "Prado.WebUI.TTimeTriggeredCallback.start('{$id}');";
			$cs = $this->getPage()->getClientScript();
			$cs->registerEndScript("{$id}:start", $code);
		}
	}

	/**
	 * @return string corresponding javascript class name for TTimeTriggeredCallback.
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TTimeTriggeredCallback';
	}
}

?>
