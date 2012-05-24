<?php
/**
 * TTriggeredCallback class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTriggeredCallback.php 2927 2011-05-28 11:43:46Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 */

Prado::using('System.Web.UI.ActiveControls.TCallback');

/**
 * TTriggeredCallback abstract Class
 *
 * Base class for triggered callback controls. The {@link setControlID ControlID}
 * property sets the control ID to observe the trigger.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TTriggeredCallback.php 2927 2011-05-28 11:43:46Z ctrlaltca@gmail.com $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
abstract class TTriggeredCallback extends TCallback
{
	/**
	 * @return string The ID of the server control the trigger is bounded to.
	 */
	public function getControlID()
	{
		return $this->getViewState('ControlID', '');
	}

	/**
	 * @param string The ID of the server control the trigger is bounded to.
	 */
	public function setControlID($value)
	{
		$this->setViewState('ControlID', $value, '');
	}

	/**
	 * @return string target control client ID or html element ID if
	 * control is not found in hierarchy.
	 */
	protected function getTargetControl()
	{
		$id = $this->getControlID();
		if(($control=$this->findControl($id)) instanceof TControl)
			return $control->getClientID();
		if($id==='')
		{
			throw new TConfigurationException(
				'ttriggeredcallback_invalid_controlid', get_class($this));
		}
		return $id;
	}

	/**
	 * @return array list of trigger callback options.
	 */
	protected function getTriggerOptions()
	{
		$options['ID'] = $this->getClientID();
		$options['EventTarget'] = $this->getUniqueID();
		$options['ControlID'] = $this->getTargetControl();
		return $options;
	}
}

