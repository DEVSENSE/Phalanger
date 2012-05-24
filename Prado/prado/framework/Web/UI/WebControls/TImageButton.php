<?php
/**
 * TImageButton class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TImageButton.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.WebControls
 */

/**
 * Includes TImage class file
 */
Prado::using('System.Web.UI.WebControls.TImage');

/**
 * TImageButton class
 *
 * TImageButton creates an image button on the page. It is used to submit data to a page.
 * You can create either a <b>submit</b> button or a <b>command</b> button.
 *
 * A <b>command</b> button has a command name (specified by
 * the {@link setCommandName CommandName} property) and and a command parameter
 * (specified by {@link setCommandParameter CommandParameter} property)
 * associated with the button. This allows you to create multiple TLinkButton
 * components on a Web page and programmatically determine which one is clicked
 * with what parameter. You can provide an event handler for
 * {@link onCommand OnCommand} event to programmatically control the actions performed
 * when the command button is clicked. In the event handler, you can determine
 * the {@link setCommandName CommandName} property value and
 * the {@link setCommandParameter CommandParameter} property value
 * through the {@link TCommandParameter::getName Name} and
 * {@link TCommandParameter::getParameter Parameter} properties of the event
 * parameter which is of type {@link TCommandEventParameter}.
 *
 * A <b>submit</b> button does not have a command name associated with the button
 * and clicking on it simply posts the Web page back to the server.
 * By default, a TImageButton control is a submit button.
 * You can provide an event handler for the {@link onClick OnClick} event
 * to programmatically control the actions performed when the submit button is clicked.
 * The coordinates of the clicking point can be obtained from the {@link onClick OnClick}
 * event parameter, which is of type {@link TImageClickEventParameter}.
 *
 * Clicking on button can trigger form validation, if
 * {@link setCausesValidation CausesValidation} is true.
 * And the validation may be restricted within a certain group of validator
 * controls by setting {@link setValidationGroup ValidationGroup} property.
 * If validation is successful, the data will be post back to the same page.
 *
 * TImageButton displays the {@link setText Text} property as the hint text to the displayed image.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageButton.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TImageButton extends TImage implements IPostBackDataHandler, IPostBackEventHandler, IButtonControl
{
	/**
	 * @var integer x coordinate that the image is being clicked at
	 */
	private $_x=0;
	/**
	 * @var integer y coordinate that the image is being clicked at
	 */
	private $_y=0;
	private $_dataChanged=false;

	/**
	 * @return string tag name of the button
	 */
	protected function getTagName()
	{
		return 'input';
	}

	/**
	 * @return boolean whether to render javascript.
	 */
	public function getEnableClientScript()
	{
		return $this->getViewState('EnableClientScript',true);
	}

	/**
	 * @param boolean whether to render javascript.
	 */
	public function setEnableClientScript($value)
	{
		$this->setViewState('EnableClientScript',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * Adds attribute name-value pairs to renderer.
	 * This overrides the parent implementation with additional button specific attributes.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	protected function addAttributesToRender($writer)
	{
		$page=$this->getPage();
		$page->ensureRenderInForm($this);
		$writer->addAttribute('type','image');
		if(($uniqueID=$this->getUniqueID())!=='')
			$writer->addAttribute('name',$uniqueID);
		if($this->getEnabled(true))
		{
			if($this->getEnableClientScript() && $this->needPostBackScript())
				$this->renderClientControlScript($writer);
		}
		else if($this->getEnabled()) // in this case, parent will not render 'disabled'
			$writer->addAttribute('disabled','disabled');
		parent::addAttributesToRender($writer);
	}

	/**
	 * Renders the client-script code.
	 */
	protected function renderClientControlScript($writer)
	{
		$writer->addAttribute('id',$this->getClientID());
		$cs = $this->getPage()->getClientScript();
		$cs->registerPostBackControl($this->getClientClassName(),$this->getPostBackOptions());
	}

	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.TImageButton';
	}

	/**
	 * @return boolean whether to perform validation if the button is clicked
	 */
	protected function canCauseValidation()
	{
		if($this->getCausesValidation())
		{
			$group=$this->getValidationGroup();
			return $this->getPage()->getValidators($group)->getCount()>0;
		}
		else
			return false;
	}

	/**
	 * @param boolean set by a panel to register this button as the default button for the panel.
	 */
	public function setIsDefaultButton($value)
	{
		$this->setViewState('IsDefaultButton', TPropertyValue::ensureBoolean($value),false);
	}

	/**
	 * @return boolean true if this button is registered as a default button for a panel.
	 */
	public function getIsDefaultButton()
	{
		return $this->getViewState('IsDefaultButton', false);
	}

	/**
	 * @return boolean whether the button needs javascript to do postback
	 */
	protected function needPostBackScript()
	{
		return $this->canCauseValidation() || $this->getIsDefaultButton();
	}

	/**
	 * Returns postback specifications for the button.
	 * This method is used by framework and control developers.
	 * @return array parameters about how the button defines its postback behavior.
	 */
	protected function getPostBackOptions()
	{
		$options['ID'] = $this->getClientID();
		$options['CausesValidation'] = $this->getCausesValidation();
		$options['ValidationGroup'] = $this->getValidationGroup();
		$options['EventTarget'] = $this->getUniqueID();

		return $options;
	}

	/**
	 * This method checks if the TImageButton is clicked and loads the coordinates of the clicking position.
	 * This method is primarly used by framework developers.
	 * @param string the key that can be used to retrieve data from the input data collection
	 * @param array the input data collection
	 * @return boolean whether the data of the component has been changed
	 */
	public function loadPostData($key,$values)
	{
		$uid=$this->getUniqueID();
		if(isset($values["{$uid}_x"]) && isset($values["{$uid}_y"]))
		{
			$this->_x=intval($values["{$uid}_x"]);
			$this->_y=intval($values["{$uid}_y"]);
			if($this->getPage()->getPostBackEventTarget()===null)
				$this->getPage()->setPostBackEventTarget($this);
			$this->_dataChanged=true;
		}
		return false;
	}

	/**
	 * A dummy implementation for the IPostBackDataHandler interface.
	 */
	public function raisePostDataChangedEvent()
	{
		// no post data to handle
	}

	/**
	 * This method is invoked when the button is clicked.
	 * The method raises 'OnClick' event to fire up the event handlers.
	 * If you override this method, be sure to call the parent implementation
	 * so that the event handler can be invoked.
	 * @param TImageClickEventParameter event parameter to be passed to the event handlers
	 */
	public function onClick($param)
	{
		$this->raiseEvent('OnClick',$this,$param);
	}

	/**
	 * This method is invoked when the button is clicked.
	 * The method raises 'OnCommand' event to fire up the event handlers.
	 * If you override this method, be sure to call the parent implementation
	 * so that the event handlers can be invoked.
	 * @param TCommandEventParameter event parameter to be passed to the event handlers
	 */
	public function onCommand($param)
	{
		$this->raiseEvent('OnCommand',$this,$param);
		$this->raiseBubbleEvent($this,$param);
	}

	/**
	 * Raises the postback event.
	 * This method is required by {@link IPostBackEventHandler} interface.
	 * If {@link getCausesValidation CausesValidation} is true, it will
	 * invoke the page's {@link TPage::validate validate} method first.
	 * It will raise {@link onClick OnClick} and {@link onCommand OnCommand} events.
	 * This method is mainly used by framework and control developers.
	 * @param TEventParameter the event parameter
	 */
	public function raisePostBackEvent($param)
	{
		if($this->getCausesValidation())
			$this->getPage()->validate($this->getValidationGroup());
		$this->onClick(new TImageClickEventParameter($this->_x,$this->_y));
		$this->onCommand(new TCommandEventParameter($this->getCommandName(),$this->getCommandParameter()));
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
	 * @return boolean whether postback event trigger by this button will cause input validation, default is true
	 */
	public function getCausesValidation()
	{
		return $this->getViewState('CausesValidation',true);
	}

	/**
	 * @param boolean whether postback event trigger by this button will cause input validation
	 */
	public function setCausesValidation($value)
	{
		$this->setViewState('CausesValidation',TPropertyValue::ensureBoolean($value),true);
	}

	/**
	 * @return string the command name associated with the {@link onCommand OnCommand} event.
	 */
	public function getCommandName()
	{
		return $this->getViewState('CommandName','');
	}

	/**
	 * @param string the command name associated with the {@link onCommand OnCommand} event.
	 */
	public function setCommandName($value)
	{
		$this->setViewState('CommandName',$value,'');
	}

	/**
	 * @return string the parameter associated with the {@link onCommand OnCommand} event
	 */
	public function getCommandParameter()
	{
		return $this->getViewState('CommandParameter','');
	}

	/**
	 * @param string the parameter associated with the {@link onCommand OnCommand} event.
	 */
	public function setCommandParameter($value)
	{
		$this->setViewState('CommandParameter',$value,'');
	}

	/**
	 * @return string the group of validators which the button causes validation upon postback
	 */
	public function getValidationGroup()
	{
		return $this->getViewState('ValidationGroup','');
	}

	/**
	 * @param string the group of validators which the button causes validation upon postback
	 */
	public function setValidationGroup($value)
	{
		$this->setViewState('ValidationGroup',$value,'');
	}

	/**
	 * @return string caption of the button
	 */
	public function getText()
	{
		return $this->getAlternateText();
	}

	/**
	 * @param string caption of the button
	 */
	public function setText($value)
	{
		$this->setAlternateText($value);
	}

	/**
	 * Registers the image button to receive postback data during postback.
	 * This is necessary because an image button, when postback, does not have
	 * direct mapping between post data and the image button name.
	 * This method overrides the parent implementation and is invoked before render.
	 * @param mixed event parameter
	 */
	public function onPreRender($param)
	{
		parent::onPreRender($param);
		$this->getPage()->registerRequiresPostData($this);
	}

	/**
	 * Renders the body content enclosed between the control tag.
	 * This overrides the parent implementation with nothing to be rendered.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function renderContents($writer)
	{
	}
}

/**
 * TImageClickEventParameter class
 *
 * TImageClickEventParameter encapsulates the parameter data for
 * {@link TImageButton::onClick Click} event of {@link TImageButton} controls.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TImageButton.php 2482 2008-07-30 02:07:13Z knut $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TImageClickEventParameter extends TEventParameter
{
	/**
	 * the X coordinate of the clicking point
	 * @var integer
	 */
	private $_x=0;
	/**
	 * the Y coordinate of the clicking point
	 * @var integer
	 */
	private $_y=0;

	/**
	 * Constructor.
	 * @param integer X coordinate of the clicking point
	 * @param integer Y coordinate of the clicking point
	 */
	public function __construct($x,$y)
	{
		$this->_x=$x;
		$this->_y=$y;
	}

	/**
	 * @return integer X coordinate of the clicking point, defaults to 0
	 */
	public function getX()
	{
		return $this->_x;
	}

	/**
	 * @param integer X coordinate of the clicking point
	 */
	public function setX($value)
	{
		$this->_x=TPropertyValue::ensureInteger($value);
	}

	/**
	 * @return integer Y coordinate of the clicking point, defaults to 0
	 */
	public function getY()
	{
		return $this->_y;
	}

	/**
	 * @param integer Y coordinate of the clicking point
	 */
	public function setY($value)
	{
		$this->_y=TPropertyValue::ensureInteger($value);
	}
}

?>
