<?php
/**
 * TActiveControlAdapter and TCallbackPageStateTracker class file.
 *
 * @author Wei Zhuo <weizhuo[at]gamil[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TActiveControlAdapter.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */

/*
 * Load common active control options.
 */
Prado::using('System.Web.UI.ActiveControls.TBaseActiveControl');

/**
 * TActiveControlAdapter class.
 *
 * Customize the parent TControl class for active control classes.
 * TActiveControlAdapter instantiates a common base active control class
 * throught the {@link getBaseActiveControl BaseActiveControl} property.
 * The type of BaseActiveControl can be provided in the second parameter in the
 * constructor. Default is TBaseActiveControl or TBaseActiveCallbackControl if
 * the control adapted implements ICallbackEventHandler.
 *
 * TActiveControlAdapter will tracking viewstate changes to update the
 * corresponding client-side properties.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveControlAdapter.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TActiveControlAdapter extends TControlAdapter
{
	/**
	 * @var string base active control class name.
	 */
	private $_activeControlType;
	/**
	 * @var TBaseActiveControl base active control instance.
	 */
	private $_baseActiveControl;
	/**
	 * @var TCallbackPageStateTracker view state tracker.
	 */
	private $_stateTracker;

	/**
	 * Constructor.
	 * @param IActiveControl active control to adapt.
	 * @param string Base active control class name.
	 */
	public function __construct(IActiveControl $control, $baseCallbackClass=null)
	{
		parent::__construct($control);
		$this->setBaseControlClass($baseCallbackClass);
	}

	/**
	 * @param string base active control instance
	 */
	protected function setBaseControlClass($type)
	{
		if($type===null)
		{
			if($this->getControl() instanceof ICallbackEventHandler)
				$this->_activeControlType = 'TBaseActiveCallbackControl';
			else
				$this->_activeControlType = 'TBaseActiveControl';
		}
		else
			$this->_activeControlType = $type;
	}

	/**
	 * Renders the callback client scripts.
	 */
	public function render($writer)
	{
		$this->renderCallbackClientScripts();
		parent::render($writer);
	}

	/**
	 * Register the callback clientscripts and sets the post loader IDs.
	 */
	protected function renderCallbackClientScripts()
	{
		$cs = $this->getPage()->getClientScript();
		$key = 'Prado.CallbackRequest.addPostLoaders';
		if(!$cs->isEndScriptRegistered($key))
		{
			$cs->registerPradoScript('ajax');
			$data = $this->getPage()->getPostDataLoaders();
			if(count($data) > 0)
			{
				$options = TJavaScript::encode($data,false);
				$script = "Prado.CallbackRequest.addPostLoaders({$options});";
				$cs->registerEndScript($key, $script);
			}
		}
	}

	/**
	 * @param TBaseActiveControl change base active control
	 */
	public function setBaseActiveControl($control)
	{
		$this->_baseActiveControl=$control;
	}

	/**
	 * @return TBaseActiveControl Common active control options.
	 */
	public function getBaseActiveControl()
	{
		if($this->_baseActiveControl===null)
		{
			$type = $this->_activeControlType;
			$this->_baseActiveControl = new $type($this->getControl());
		}
		return $this->_baseActiveControl;
	}

	/**
	 * @return boolean true if the viewstate needs to be tracked.
	 */
	protected function getIsTrackingPageState()
	{
		if($this->getPage()->getIsCallback())
		{
			$target = $this->getPage()->getCallbackEventTarget();
			if($target instanceof ICallbackEventHandler)
			{
				$client = $target->getActiveControl()->getClientSide();
				return $client->getEnablePageStateUpdate();
			}
		}
		return false;
	}

	/**
	 * Starts viewstate tracking if necessary after when controls has been loaded
	 */
	public function onLoad($param)
	{
		if($this->getIsTrackingPageState())
		{
			$this->_stateTracker = new TCallbackPageStateTracker($this->getControl());
			$this->_stateTracker->trackChanges();
		}
		parent::onLoad($param);
	}

	/**
	 * Saves additional persistent control state. Respond to viewstate changes
	 * if necessary.
	 */
	public function saveState()
	{
		if(($this->_stateTracker!==null)
			&& $this->getControl()->getActiveControl()->canUpdateClientSide())
		{
			$this->_stateTracker->respondToChanges();
		}
		parent::saveState();
	}

	/**
	 * @return TCallbackPageStateTracker state tracker.
	 */
	public function getStateTracker()
	{
		return $this->_stateTracker;
	}
}

/**
 * TCallbackPageStateTracker class.
 *
 * Tracking changes to the page state during callback.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveControlAdapter.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TCallbackPageStateTracker
{
	/**
	 * @var TMap new view state data
	 */
	private $_states;
	/**
	 * @var TMap old view state data
	 */
	private $_existingState;
	/**
	 * @var TControl the control tracked
	 */
	private $_control;
	/**
	 * @var object null object.
	 */
	private $_nullObject;

	/**
	 * Constructor. Add a set of default states to track.
	 * @param TControl control to track.
	 */
	public function __construct($control)
	{
		$this->_control = $control;
		$this->_existingState = new TMap;
		$this->_nullObject = new stdClass;
		$this->_states = new TMap;
		$this->addStatesToTrack();
	}

	/**
	 * Add a list of view states to track. Each state is added
	 * to the StatesToTrack property with the view state name as key.
	 * The value should be an array with two enteries. The first entery
	 * is the name of the class that will calculate the state differences.
	 * The second entry is a php function/method callback that handles
	 * the changes in the viewstate.
	 */
	protected function addStatesToTrack()
	{
		$states = $this->getStatesToTrack();
		$states['Visible'] = array('TScalarDiff', array($this, 'updateVisible'));
		$states['Enabled'] = array('TScalarDiff', array($this, 'updateEnabled'));
		$states['Attributes'] = array('TMapCollectionDiff', array($this, 'updateAttributes'));
		$states['Style'] = array('TStyleDiff', array($this, 'updateStyle'));
		$states['TabIndex'] = array('TScalarDiff', array($this, 'updateTabIndex'));
		$states['ToolTip'] = array('TScalarDiff', array($this, 'updateToolTip'));
		$states['AccessKey'] = array('TScalarDiff', array($this, 'updateAccessKey'));
	}

	/**
	 * @return TMap list of viewstates to track.
	 */
	protected function getStatesToTrack()
	{
		return $this->_states;
	}

	/**
	 * Start tracking view state changes. The clone function on objects are called
	 * for those viewstate having an object as value.
	 */
	public function trackChanges()
	{
		foreach($this->_states as $name => $value)
		{
			$obj = $this->_control->getViewState($name);
			$this->_existingState[$name] = is_object($obj) ? clone($obj) : $obj;
		}
	}

	/**
	 * @return array list of viewstate and the changed data.
	 */
	protected function getChanges()
	{
		$changes = array();
		foreach($this->_states as $name => $details)
		{
			$new = $this->_control->getViewState($name);
			$old = $this->_existingState[$name];
			if($new !== $old)
			{
				$diff = new $details[0]($new, $old, $this->_nullObject);
				if(($change = $diff->getDifference()) !== $this->_nullObject)
					$changes[] = array($details[1],array($change));
			}
		}
		return $changes;
	}

	/**
	 * For each of the changes call the corresponding change handlers.
	 */
	public function respondToChanges()
	{
		foreach($this->getChanges() as $change)
			call_user_func_array($change[0], $change[1]);
	}

	/**
	 * @return TCallbackClientScript callback client scripting
	 */
	protected function client()
	{
		return $this->_control->getPage()->getCallbackClient();
	}

	/**
	 * Updates the tooltip.
	 * @param string new tooltip
	 */
	protected function updateToolTip($value)
	{
		$this->client()->setAttribute($this->_control, 'title', $value);
	}

	/**
	 * Updates the tab index.
	 * @param integer tab index
	 */
	protected function updateTabIndex($value)
	{
		$this->client()->setAttribute($this->_control, 'tabindex', $value);
	}

	/**
	 * Updates the modifier access key
	 * @param string access key
	 */
	protected function updateAccessKey($value)
	{
		$this->client()->setAttribute($this->_control, 'accesskey', $value);
	}

	/**
	 * Hides or shows the control on the client-side. The control must be
	 * already rendered on the client-side.
	 * @param boolean true to show the control, false to hide.
	 */
	protected function updateVisible($visible)
	{
		if($visible === false)
			$this->client()->hide($this->_control);
		else
			$this->client()->show($this->_control);
	}

	/**
	 * Enables or Disables the control on the client-side.
	 * @param boolean true to enable the control, false to disable.
	 */
	protected function updateEnabled($enable)
	{
		$this->client()->setAttribute($this->_control, 'disabled', $enable===false);
	}

	/**
	 * Updates the CSS style on the control on the client-side.
	 * @param array list of new CSS style declarations.
	 */
	protected function updateStyle($style)
	{
		if($style['CssClass']!==null)
			$this->client()->setAttribute($this->_control, 'class', $style['CssClass']);
		if(count($style['Style']) > 0)
			$this->client()->setStyle($this->_control, $style['Style']);
	}

	/**
	 * Updates/adds a list of attributes on the control.
	 * @param array list of attribute name-value pairs.
	 */
	protected function updateAttributes($attributes)
	{
		foreach($attributes as $name => $value)
			$this->client()->setAttribute($this->_control, $name, $value);
	}
}

/**
 * Calculates the viewstate changes during the request.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveControlAdapter.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
abstract class TViewStateDiff
{
	/**
	 * @var mixed updated viewstate
	 */
	protected $_new;
	/**
	 * @var mixed viewstate value at the begining of the request.
	 */
	protected $_old;
	/**
	 * @var object null value.
	 */
	protected $_null;

	/**
	 * Constructor.
	 * @param mixed updated viewstate value.
	 * @param mixed viewstate value at the begining of the request.
	 * @param object representing the null value.
	 */
	public function __construct($new, $old, $null)
	{
		$this->_new = $new;
		$this->_old = $old;
		$this->_null = $null;
	}

	/**
	 * @return mixed view state changes, nullObject if no difference.
	 */
	public abstract function getDifference();
}

/**
 * TScalarDiff class.
 *
 * Calculate the changes to a scalar value.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveControlAdapter.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TScalarDiff extends TViewStateDiff
{
	/**
	 * @return mixed update viewstate value.
	 */
	public function getDifference()
	{
		if(gettype($this->_new) === gettype($this->_old)
			&& $this->_new === $this->_old)
			return $this->_null;
		else
			return $this->_new;
	}
}

/**
 * TStyleDiff class.
 *
 * Calculates the changes to the Style properties.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveControlAdapter.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TStyleDiff extends TViewStateDiff
{
	/**
	 * @param TStyle control style
	 * @return array all the style properties combined.
	 */
	protected function getCombinedStyle($obj)
	{
		if(!($obj instanceof TStyle))
			return array();
		$style = $obj->getStyleFields();
		$style = array_merge($style,$this->getStyleFromString($obj->getCustomStyle()));
		if($obj->hasFont())
			$style = array_merge($style, $this->getStyleFromString($obj->getFont()->toString()));
		return $style;
	}

	/**
	 * @param string CSS custom style string.
	 * @param array CSS style as name-value array.
	 */
	protected function getStyleFromString($string)
	{
		$style = array();
		if(!is_string($string)) return $style;

		foreach(explode(';',$string) as $sub)
		{
			$arr=explode(':',$sub);
			if(isset($arr[1]) && trim($arr[0])!=='')
				$style[trim($arr[0])] = trim($arr[1]);
		}
		return $style;
	}

	/**
	 * @return string changes to the CSS class name.
	 */
	protected function getCssClassDiff()
	{
		if($this->_old===null)
		{
			return ($this->_new!==null) && $this->_new->hasCssClass()
						? $this->_new->getCssClass() : null;
		}
		else
		{
			return $this->_old->getCssClass() !== $this->_new->getCssClass() ?
				$this->_new->getCssClass() : null;
		}
	}

	/**
	 * @return array list of changes to the control style.
	 */
	protected function getStyleDiff()
	{
		$diff = array_diff_assoc(
					$this->getCombinedStyle($this->_new),
					$this->getCombinedStyle($this->_old));
		return count($diff) > 0 ? $diff : null;
	}

	/**
	 * @return array list of changes to the control style and CSS class name.
	 */
	public function getDifference()
	{
		if($this->_new===null)
			return $this->_null;
		else
		{
			$css = $this->getCssClassDiff();
			$style = $this->getStyleDiff();
			if(($css!==null) || ($style!==null))
				return array('CssClass' => $css, 'Style' => $style);
			else
				$this->_null;
		}
	}
}

/**
 * TAttributesDiff class.
 *
 * Calculate the changes to attributes collection.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TActiveControlAdapter.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 * @since 3.1
 */
class TMapCollectionDiff extends TViewStateDiff
{
	/**
	 * @return array updates to the attributes collection.
	 */
	public function getDifference()
	{
		if($this->_old===null)
		{
			return ($this->_new!==null) ? $this->_new->toArray() : $this->_null;
		}
		else
		{
			$new = $this->_new->toArray();
			$old = $this->_old->toArray();
			$diff = array_diff_assoc($new, $old);
			return count($diff) > 0 ? $diff : $this->_null;
		}
	}
}

