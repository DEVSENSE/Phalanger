<?php
/**
 * TSlider class file.
 *
 * @author Christophe Boulain <Christophe.Boulain@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TSlider.php 2564 2008-11-11 21:56:02Z carlgmathisen $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */

/**
 * TSlider class
 *
 * TSlider displays a slider for numeric input purpose. A slider consists of a 'track',
 * which define the range of possible value, and a 'handle' which can slide on the track, to select
 * a value in the range. The track can be either Horizontal or Vertical, depending of the {@link SetDirection Direction}
 * property. By default, it's horizontal.
 *
 * The range boundaries are defined by {@link SetMinValue MinValue} and {@link SetMaxValue MaxValue} properties.
 * The default range is from 0 to 100.
 * The {@link SetStepSize StepSize} property can be used to define the <b>step</b> between 2 values inside the range.
 * Notice that this step will be recomputed if there is more than 200 values between the range boundaries.
 * You can also provide the allowed values by setting the {@link SetValues Values} array.
 *
 * A 'Progress Indicator' can be displayed within the track with the {@link SetProgressIndicator ProgressIndicator} property.
 * 
 * The TSlider control can be easily customized using CssClasses. You can provide your own css file, using the
 * {@link SetCssUrl CssUrl} property.
 * The css class for TSlider can be set by the {@link setCssClass CssClass} property. Default value is "Slider HorizontalSlider" 
 * for an horizontal slider, and "Slider VerticalSlider" for a vertical one.
 *
 * If {@link SetAutoPostBack AutoPostBack} property is true, postback is sent as soon as the value changed.
 *
 * TSlider raises the {@link onValueChanged} event when the value of the slider has changed during postback.
 *
 * You can also attach ClientSide javascript events handler to the slider :
 * - ClientSide.onSlide is called when the handle is slided on the track. You can get the current value in the <b>value</b>
 * javascript variable. You can use this event to update on client side a label with the current value
 * - ClientSide.onChange is called when the slider value has changed (at the end of a move).
 *
 * @author Christophe Boulain <Christophe.Boulain@gmail.com>
 * @version $Id: TSlider.php 2564 2008-11-11 21:56:02Z carlgmathisen $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TSlider extends TWebControl implements IPostBackDataHandler, IDataRenderer
{
	const MAX_STEPS=200;
	/**
	 * @var TSliderHandle handle component
	 */
	private $_handle;
	/*
	 * @var boolean Wether the data has changed during postback
	 */
	private $_dataChanged=false;
	/**
	 * @var TSliderClientScript Clients side javascripts
	 */
	private $_clientScript=null;

	/**
	 * @return TSliderDirection Direction of slider (Horizontal or Vertical). Defaults to Horizontal.
	 */
	public function getDirection()
	{
		return $this->getViewState('Direction', TSliderDirection::Horizontal);
	}

	/**
	 * @param TSliderDirection Direction of slider (Horizontal or Vertical)
	 */
	public function setDirection($value)
	{
		$this->setViewState('Direction', TPropertyValue::ensureEnum($value,'TSliderDirection'),TSliderDirection::Horizontal);
	}

	/**
	 * @return string URL for the CSS file including all relevant CSS class definitions. Defaults to '' (a default CSS file will be applied in this case.)
	 */
	public function getCssUrl()
	{
		return $this->getViewState('CssUrl','');
	}

	/**
	 * @param string URL for the CSS file including all relevant CSS class definitions.
	 */
	public function setCssUrl($value)
	{
		$this->setViewState('CssUrl',TPropertyValue::ensureString($value),'');
	}

	/**
	 * @return float Maximum value for the slider. Defaults to 100.0.
	 */
	public function getMaxValue()
	{
		return $this->getViewState('MaxValue',100.0);
	}

	/**
	 * @param float Maximum value for slider
	 */
	public function setMaxValue($value)
	{
		$this->setViewState('MaxValue', TPropertyValue::ensureFloat($value),100.0);
	}

	/**
	 * @return float Minimum value for slider. Defaults to 0.0.
	 */
	public function getMinValue()
	{
		return $this->getViewState('MinValue',0.0);
	}

	/**
	 * @param float Minimum value for slider
	 */
	public function setMinValue($value)
	{
		$this->setViewState('MinValue', TPropertyValue::ensureFloat($value),0.0);
	}

	/**
	 * @return float Step size. Defaults to 1.0.
	 */
	public function getStepSize()
	{
		return $this->getViewState('StepSize', 1.0);
	}

	/**
	 * Sets the step size used to determine the places where the slider handle can stop at.
	 * An evenly distributed stop marks will be generated according to
	 * {@link getMinValue MinValue}, {@link getMaxValue MaxValue} and StepSize.
	 * To use uneven stop marks, set {@link setValues Values}.
	 * @param float Step size.
	 */
	public function setStepSize($value)
	{
		$this->setViewState('StepSize', $value, 1.0);
	}
	
	/**
	 * @return boolean wether to display a progress indicator or not. Defaults to true.
	 */
	public function getProgressIndicator ()
	{
		return $this->getViewState('ProgressIndicator', true);
	}

	/**
	 * @param boolean wether to display a progress indicator or not. Defaults to true.
	 */
	public function setProgressIndicator ($value)
	{
		$this->setViewState('ProgressIndicator', TPropertyValue::ensureBoolean($value), true);
	}
	/**
	 * @return float current value of slider
	 */
	public function getValue()
	{
		return $this->getViewState('Value',0.0);
	}

	/**
	 * @param float current value of slider
	 */
	public function setValue($value)
	{
		$this->setViewState('Value', TPropertyValue::ensureFloat($value),0.0);
	}

	/**
	 * Returns the value of the TSlider control.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link getValue()}.
	 * @return string the value of the TSlider control.
	 * @see getValue
	 */
	public function getData()
	{
		return $this->getValue();
	}

	/**
	 * Sets the value of the TSlider control.
	 * This method is required by {@link IDataRenderer}.
	 * It is the same as {@link setValue()}.
	 * @param string the value of the TSlider control.
	 * @see setValue
	 */
	public function setData($value)
	{
		$this->setValue($value);
	}

	/**
	 * @return array list of allowed values the slider can take. Defaults to an empty array.
	 */
	public function getValues()
	{
		return $this->getViewState('Values', array());
	}

	/**
	 * Sets the possible values that the slider can take.
	 * If this is set, {@link setStepSize StepSize} will be ignored. The latter
	 * generates a set of evenly distributed candidate values.
	 * @param array list of allowed values the slider can take
	 */
	public function setValues($value)
	{
		$this->setViewState('Values', TPropertyValue::ensureArray($value), array());
	}
	
	/**
	 * @return boolean a value indicating whether an automatic postback to the server
	 * will occur whenever the user modifies the slider value. Defaults to false.
	 */
	public function getAutoPostBack()
	{
		return $this->getViewState('AutoPostBack',false);
	}

	/**
	 * Sets the value indicating if postback automatically.
	 * An automatic postback to the server will occur whenever the user
	 * modifies the slider value.
	 * @param boolean the value indicating if postback automatically
	 */
	public function setAutoPostBack($value)
	{
		$this->setViewState('AutoPostBack',TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TSlider';
	}

	/**
	 * Returns a value indicating whether postback has caused the control data change.
	 * This method is required by the IPostBackDataHandler interface.
	 * @return boolean whether postback has caused the control data change. False if the page is not in postback mode.
	 */
	public function getDataChanged()
	{
		return $this->_dataChanged;
	}

	/**
	 * Raises postdata changed event.
	 * This method is required by {@link IPostBackDataHandler} interface.
	 * It is invoked by the framework when {@link getValue Value} property
	 * is changed on postback.
	 * This method is primarly used by framework developers.
	 */
	public function raisePostDataChangedEvent()
	{
		$this->onValueChanged(null);
	}

	/**
	 * Raises <b>OnValueChanged</b> event.
	 * This method is invoked when the {@link getValue Value}
	 * property changes on postback.
	 * If you override this method, be sure to call the parent implementation to ensure
	 * the invocation of the attached event handlers.
	 * @param TEventParameter event parameter to be passed to the event handlers
	 */
	public function onValueChanged($param)
	{
		$this->raiseEvent('OnValueChanged',$this,$param);
	}

	/**
	 * Loads user input data.
	 * This method is primarly used by framework developers.
	 * @param string the key that can be used to retrieve data from the input data collection
	 * @param array the input data collection
	 * @return boolean whether the data of the component has been changed
	 */
	public function loadPostData($key,$values)
	{
		$value=(float)$values[$this->getClientID().'_1'];
		if($this->getValue()!==$value)
		{
			$this->setValue($value);
			return $this->_dataChanged=true;
		}
		else
			return false;
	}

	/**
	 * Gets the TSliderClientScript to set the TSlider event handlers.
	 *
	 * The slider on the client-side supports the following events.
	 * # <tt>OnSliderMove</tt> -- raised when the slider is moved.
	 * # <tt>OnSliderChanged</tt> -- raised when the slider value is changed
	 *
	 * You can attach custom javascript code to each of these events
	 *
	 * @return TSliderClientScript javascript validator event options.
	 */
	public function getClientSide()
	{
		if($this->_clientScript===null)
			$this->_clientScript = $this->createClientScript();
		return $this->_clientScript;
	}

	/**
	 * @return TSliderClientScript javascript event options.
	 */
	protected function createClientScript()
	{
		return new TSliderClientScript;
	}

	/**
	 * @return string the HTML tag name for slider. Defaults to div.
	 */
	public function getTagName ()
	{
		return "div";
	}

	/**
	 * Add the specified css classes to the track
	 * @param THtmlWriter writer
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		$writer->addAttribute('id',$this->getClientID());
		if ($this->getCssClass()==='')
		{
			$class=($this->getDirection()==TSliderDirection::Horizontal)?'HorizontalSlider':'VerticalSlider';
			$writer->addAttribute('class', 'Slider '.$class);
		}
		
	}

	/**
	 * Render the body content
	 */
	public function renderContents($writer)
	{
		// Render the 'Track'
		$writer->addAttribute('class', 'Track');
		$writer->addAttribute('id', $this->getClientID().'_track');
		$writer->renderBeginTag('div');
		// Render the 'Progress Indicator'
		if ($this->getProgressIndicator())
		{
			$writer->addAttribute('class', 'Progress');
			$writer->addAttribute('id', $this->getClientID().'_progress');
			$writer->renderBeginTag('div');
			$writer->renderEndTag();
		}
		// Render the 'Ruler'
		/*
		 * Removing for now
		$writer->addAttribute('class', 'RuleContainer');
		$writer->addAttribute('id', $this->getClientID()."_rule");
		$writer->renderBeginTag('div');
		for ($i=0;$i<=100;$i+=10)
		{
			$writer->addAttribute('class', 'RuleMark');
			$attr=($this->getDirection()===TSliderDirection::Horizontal)?"left":"top";
			$writer->addStyleAttribute($attr, $i.'%');
			$writer->renderBeginTag('div');
			$writer->renderEndTag();
		}
		$writer->renderEndTag();
		*/
		
		$writer->renderEndTag();
		
		// Render the 'Handle'
		$writer->addAttribute('class', 'Handle');
		$writer->addAttribute('id', $this->getClientID().'_handle');
		$writer->renderBeginTag('div');
		$writer->renderEndTag();
	}
	/**
	 * Registers CSS and JS.
	 * This method is invoked right before the control rendering, if the control is visible.
	 * @param mixed event parameter
	 */
	public function onPreRender ($param)
	{
		parent::onPreRender($param);
		$this->registerStyleSheet();
		$this->registerSliderClientScript();

	}

	/**
	 * Registers the CSS relevant to the TSlider.
	 * It will register the CSS file specified by {@link getCssUrl CssUrl}.
	 * If that is not set, it will use the default CSS.
	 */
	protected function registerStyleSheet()
	{
		if(($url=$this->getCssUrl())==='')
		{
			$manager=$this->getApplication()->getAssetManager();
			// publish the assets
			$url=$manager->publishFilePath(dirname(__FILE__).DIRECTORY_SEPARATOR.'assets'.DIRECTORY_SEPARATOR.'TSlider');
			$url.='/TSlider.css';
		}
		$this->getPage()->getClientScript()->registerStyleSheetFile($url,$url);
	}

	/**
	 * Registers the javascript code to initialize the slider.
	 */
	protected function registerSliderClientScript()
	{
		$page=$this->getPage();
		$cs = $page->getClientScript();
		$cs->registerPradoScript("slider");
		$id=$this->getClientID();
		$cs->registerHiddenField($id.'_1',$this->getValue());
		$page->registerRequiresPostData($this);
		$cs->registerPostBackControl($this->getClientClassName(),$this->getSliderOptions());
	}

	/**
	 * Get javascript sliderr options.
	 * @return array slider client-side options
	 */
	protected function getSliderOptions()
	{
		// PostBack Options :
		$options['ID'] = $this->getClientID();
		$options['EventTarget'] = $this->getUniqueID();
		$options['AutoPostBack'] = $this->getAutoPostBack();

		// Slider Control options
		$minValue=$this->getMinValue();
		$maxValue=$this->getMaxValue();
		$options['axis'] = strtolower($this->getDirection());
		$options['maximum'] = $maxValue;
		$options['minimum'] = $minValue;
		$options['range'] = 'javascript:$R('.$minValue.",".$maxValue.")";
		$options['sliderValue'] = $this->getValue();
		$options['disabled'] = !$this->getEnabled();
		$values=$this->getValues();
		if (!empty($values))
		{
			// Values are provided. Check if min/max are present in them
			if (!in_array($minValue, $values)) $values[]=$minValue;
			if (!in_array($maxValue, $values)) $values[]=$maxValue;
			// Remove all values outsize the range [min..max]
			foreach ($values as $idx=>$value)
			{
				if ($value < $minValue) unset ($values[$idx]);
				if ($value > $maxValue) unset ($values[$idx]);
			}
		}
		else
		{
			// Values are not provided, generate automatically using stepsize
			$step=$this->getStepSize();
			// We want at most self::MAX_STEPS values, so, change the step if necessary
			if (($maxValue-$minValue)/$step > self::MAX_STEPS)
			{
				$step=($maxValue-$minValue)/self::MAX_STEPS;
			}
			$values=array();
			for ($i=$minValue;$i<=$maxValue;$i+=$step)
				$values[]=$i;
			// Add max if it's not in the array because of step
			if (!in_array($maxValue, $values)) $values[]=$maxValue;
		}
		$options['values'] = TJavaScript::Encode($values,false);
		if($this->_clientScript!==null)
			$options = array_merge($options,$this->_clientScript->getOptions()->toArray());
		return $options;
	}
}

/**
 * TSliderClientScript class.
 *
 * Client-side slider events {@link setOnChange OnChange} and {@line setOnMove OnMove}
 * can be modified through the {@link TSlider:: getClientSide ClientSide}
 * property of a slider.
 *
 * The current value of the slider can be get in the 'value' js variable
 *
 * The <tt>OnMove</tt> event is raised when the slider moves
 * The <tt>OnChange</tt> event is raised when the slider value is changed (or at the end of a move)
 *
 * @author Christophe Boulain <Christophe.Boulain@gmail.com>
 * @version $Id: TSlider.php 2564 2008-11-11 21:56:02Z carlgmathisen $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TSliderClientScript extends TClientSideOptions
{
	/**
	 * Javascript code to execute when the slider value is changed.
	 * @param string javascript code
	 */
	public function setOnChange($javascript)
	{
		$code="javascript: function (value) { {$javascript} }";
		$this->setFunction('onChange', $code);
	}

	/**
	 * @return string javascript code to execute when the slider value is changed.
	 */
	public function getOnChange()
	{
		return $this->getOption('onChange');
	}

	/* Javascript code to execute when the slider moves.
	 * @param string javascript code
	 */
	public function setOnSlide($javascript)
	{
		$code="javascript: function (value) { {$javascript} }";
		$this->setFunction('onSlide', $code);
	}

	/**
	 * @return string javascript code to execute when the slider moves.
	 */
	public function getOnSlide()
	{
		return $this->getOption('onSlide');
	}
}


/**
 * TSliderDirection class.
 *
 * TSliderDirection defines the enumerable type for the possible direction that can be used in a {@link TSlider}
 *
 * The following enumerable values are defined :
 * - Horizontal : Horizontal slider
 * - Vertical : Vertical slider
 *
 * @author Christophe Boulain <Christophe.Boulain@gmail.com>
 * @version $Id: TSlider.php 2564 2008-11-11 21:56:02Z carlgmathisen $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TSliderDirection extends TEnumerable
{
	const Horizontal='Horizontal';
	const Vertical='Vertical';
}


