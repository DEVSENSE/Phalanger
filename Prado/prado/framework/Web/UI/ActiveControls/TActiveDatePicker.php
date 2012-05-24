<?php
/**
 * TActiveDatePicker class file
 * 
 * @author Bradley Booms <Bradley.Booms@nsighttel.com>
 * @author Christophe Boulain <Christophe.Boulain@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveDatePicker.php 2633 2009-04-08 07:10:01Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active control adapter.
 */
Prado::using('System.Web.UI.ActiveControls.TActiveControlAdapter');

/**
 * TActiveDatePicker class
 * 
 * The active control counter part to date picker control.
 * When the date selection is changed, the {@link onCallback OnCallback} event is
 * raised.
 * 
 * @author Bradley Booms <Bradley.Booms@nsighttel.com>
 * @author Christophe Boulain <Christophe.Boulain@gmail.com>
 * @version $Id: TActiveDatePicker.php 2633 2009-04-08 07:10:01Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1.3
 */
class TActiveDatePicker extends TDatePicker  implements ICallbackEventHandler, IActiveControl {
	

	/**
	 * Get javascript date picker options.
	 * @return array date picker client-side options
	 */
	protected function getDatePickerOptions(){
		$options = parent::getDatePickerOptions();
		$options['EventTarget'] = $this->getUniqueID();
		$options['ShowCalendar'] = $this->getShowCalendar();
		return $options;
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
	}
	
	/**
	 * @return TBaseActiveCallbackControl standard callback control options.
	 */
	public function getActiveControl(){
		return $this->getAdapter()->getBaseActiveControl();
	}

	/**
	 * Client-side Text property can only be updated after the OnLoad stage.
	 * @param string text content for the textbox
	 */
	public function setText($value){
		parent::setText($value);
		if($this->getActiveControl()->canUpdateClientSide() && $this->getHasLoadedPostData()){
			$cb=$this->getPage()->getCallbackClient();
			$cb->setValue($this, $value);
			if ($this->getInputMode()==TDatePickerInputMode::DropDownList)
			{
				$s = Prado::createComponent('System.Util.TDateTimeStamp');
				$date = $s->getDate($this->getTimeStampFromText());
				$id=$this->getClientID();
				$cb->select($id.TControl::CLIENT_ID_SEPARATOR.'day', 'Value', $date['mday'], 'select');
				$cb->select($id.TControl::CLIENT_ID_SEPARATOR.'month', 'Value', $date['mon']-1, 'select');
				$cb->select($id.TControl::CLIENT_ID_SEPARATOR.'year', 'Value', $date['year'], 'select');
				
			}
		}
	}
	
	/**
	 * Raises the callback event. This method is required by {@link
	 * ICallbackEventHandler} interface. 
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the event parameter
	 */
 	public function raiseCallbackEvent($param){
		$this->onCallback($param);
	}	
	
	/**
	 * This method is invoked when a callback is requested. The method raises
	 * 'OnCallback' event to fire up the event handlers. If you override this
	 * method, be sure to call the parent implementation so that the event
	 * handler can be invoked.
	 * @param TCallbackEventParameter event parameter to be passed to the event handlers
	 */
	public function onCallback($param){
		$this->raiseEvent('OnCallback', $this, $param);
	}
	
	/**
	 * Registers the javascript code to initialize the date picker.
	 */
	protected function registerCalendarClientScript()
	{
	
		$cs = $this->getPage()->getClientScript();
		$cs->registerPradoScript("activedatepicker");

		if(!$cs->isEndScriptRegistered('TDatePicker.spacer'))
		{
			$spacer = $this->getAssetUrl('spacer.gif');
			$code = "Prado.WebUI.TDatePicker.spacer = '$spacer';";
			$cs->registerEndScript('TDatePicker.spacer', $code);
		}

		$options = TJavaScript::encode($this->getDatePickerOptions());
		$code = "new Prado.WebUI.TActiveDatePicker($options);";
		$cs->registerEndScript("prado:".$this->getClientID(), $code);
	}
}
?>
