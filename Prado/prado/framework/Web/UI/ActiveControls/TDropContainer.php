<?php
/**
 * TDropContainer class file
 * 
 * @author Christophe BOULAIN (Christophe.Boulain@gmail.com)
 * @copyright Copyright &copy; 2008, PradoSoft
 * @license http://www.pradosoft.com/license
 * @version $Id: TDropContainer.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */

/**
 * Load active control adapter.
 */
Prado::using('System.Web.UI.ActiveControls.TActiveControlAdapter');
/**
 * Load active panel.
 */
Prado::using('System.Web.UI.ActiveControls.TActivePanel');


/**
 * TDropContainer is a panel where TDraggable controls can be dropped.
 * When a TDraggable component is dropped into a TDropContainer, the {@link OnDrop OnDrop} event is raised.
 * The {@link TDropContainerEventParameter} param will contain the dropped control. 
 * 
 * Properties :
 * 
 * <b>{@link setAcceptCssClass AcceptCssClass}</b> : a coma delimited classname of elements that the drop container can accept.
 * <b>{@link setHoverCssClass HoverCssClass}</b>: CSS classname of the container when a draggable element hovers over the container.
 * 
 * Events:
 * 
 * <b>{@link OnDrop OnDrop}</b> : raised when a TDraggable control is dropped. The dropped control is encapsulated in the event parameter
 * 
 * @author Christophe BOULAIN (Christophe.Boulain@gmail.com)
 * @copyright Copyright &copy; 2008, PradoSoft
 * @license http://www.pradosoft.com/license
 * @version $Id: TDropContainer.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */
class TDropContainer extends TPanel implements IActiveControl, ICallbackEventHandler 
{	
	private $_container=null;
	
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
	 * @return TBaseActiveControl standard active control options.
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
	 * Gets the Css class name that this container can accept.
	 * @return string
	 */
	public function getAcceptCssClass()
	{
		return $this->getViewState('Accepts', '');
	}

	/**
	 * Sets the Css class name that this container can accept.
	 * @param string comma delimited css class names.
	 */
	public function setAcceptCssClass($value)
	{
		$this->setViewState('Accepts', TPropertyValue::ensureArray($value), '');
	}
	
	/**
	 * Sets the Css class name used when a draggble element is hovering
	 * over this container.
	 * @param string css class name during draggable hover.
	 */
	public function setHoverCssClass($value)
	{
		$this->setViewState('HoverClass', $value, '');
	}

	/**
	 * Gets the Css class name used when a draggble element is hovering
	 * over this container.
	 * @return string css class name during draggable hover.
	 */
	public function getHoverCssClass()
	{
		return $this->getViewState('HoverClass', '');
	}
	
	
	/**
	 * Raises callback event. This method is required bu {@link ICallbackEventHandler}
	 * interface.
	 * It raises the {@link onDrop OnDrop} event, then, the {@link onCallback OnCallback} event
	 * This method is mainly used by framework and control developers.
	 * @param TCallbackEventParameter the parameter associated with the callback event
	 */
	public function raiseCallbackEvent($param)
	{
		$this->onDrop($param->getCallbackParameter());
		$this->onCallback($param);
	}
	
	/**
	 * Raises the onDrop event. 
	 * The dropped control is encapsulated into a {@link TDropContainerEventParameter}
	 * 
	 * @param string $dropControlId
	 */
	public function onDrop ($dropControlId)
	{
		// Find the control
		// Warning, this will not work if you have a '_' in your control Id !
		$dropControlId=str_replace(TControl::CLIENT_ID_SEPARATOR,TControl::ID_SEPARATOR,$dropControlId);
		$control=$this->getPage()->findControl($dropControlId);
		$this->raiseEvent('OnDrop', $this, new TDropContainerEventParameter ($control));
		
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
	 * Gets the post back options for this textbox.
	 * @return array
	 */
	protected function getPostBackOptions()
	{
		$options['ID'] = $this->getClientID();
		$options['EventTarget'] = $this->getUniqueID();

		$options['accept'] = TJavascript::encode($this->getAcceptCssClass());
		$options['hoverclass'] = $this->getHoverCssClass();
		return $options;
	}
	
	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName()
	{
		return 'Prado.WebUI.DropContainer';
	}	


	/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control.
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		$writer->addAttribute('id',$this->getClientID());

		$this->getPage()->getClientScript()->registerPradoScript('dragdrop');

		$this->getActiveControl()->registerCallbackClientScript(
			$this->getClientClassName(), $this->getPostBackOptions());
	}
	
	/**
	 * Creates child control
	 * Override parent implementation to create a container which will contain all
	 * child controls. This container will be a TActivePanel, in order to allow user
	 * to update its content on callback.
	 */
	public function createChildControls ()
	{
		if ($this->_container===null)
		{
			$this->_container=Prado::CreateComponent('System.Web.UI.ActiveControls.TActivePanel');
			$this->_container->setId($this->getId(false).'_content');
			parent::getControls()->add($this->_container);
		}
	}
	
	/**
	 * Override parent implementation to return the container control collection.
	 *
	 * @return TControlCollection
	 */
	public function getControls()
	{
		$this->ensureChildControls();
		return $this->_container->getControls();
	}
	
	/**
	 * Renders and replaces the panel's content on the client-side.
	 * When render() is called before the OnPreRender event, such as when render()
	 * is called during a callback event handler, the rendering
	 * is defered until OnPreRender event is raised.
	 * @param THtmlWriter html writer
	 */
	public function render ($writer)
	{
		if($this->getHasPreRendered())
		{
			parent::render($writer);
			if($this->getActiveControl()->canUpdateClientSide())
				$this->getPage()->getCallbackClient()->replaceContent($this->_container,$writer);
		}
		else
		{
			$this->getPage()->getAdapter()->registerControlToRender($this->_container,$writer);
		}
	}
			
}

/**
 * TDropContainerEventParameter class
 * 
 * TDropContainerEventParameter encapsulate the parameter
 * data for <b>OnDrop</b> event of TDropContainer components
 * 
 * @author Christophe BOULAIN (Christophe.Boulain@ceram.fr)
 * @copyright Copyright &copy; 2008, PradoSoft
 * @license http://www.pradosoft.com/license
 * @version $Id: TDropContainer.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 * @package System.Web.UI.ActiveControls
 */
class TDropContainerEventParameter extends TEventParameter
{
	/*
	 * the id of control which has been dropped
	 * @var string
	 */
	private $_droppedControl;
	
	/**
	 * constructor
	 *
	 * @param string the id of control which been dropped
	 */
	public function __construct ($control)
	{
		$this->_droppedControl=$control;
	}
	
	/**
	 * @return TDraggable 
	 */
	public function getDroppedControl ()
	{
		return $this->_droppedControl;
	}
}
?>