<?php
/**
 * TMultiView and TView class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TMultiView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TMultiView class
 *
 * TMultiView serves as a container for a group of {@link TView} controls.
 * The view collection can be retrieved by {@link getViews Views}.
 * Each view contains child controls. TMultiView determines which view and its
 * child controls are visible. At any time, at most one view is visible (called
 * active). To make a view active, set {@link setActiveView ActiveView} or
 * {@link setActiveViewIndex ActiveViewIndex}.
 *
 * TMultiView also responds to specific command events raised from button controls
 * contained in current active view. A command event with name 'NextView'
 * will cause TMultiView to make the next available view active.
 * Other command names recognized by TMultiView include
 * - PreviousView : switch to previous view
 * - SwitchViewID : switch to a view by its ID path
 * - SwitchViewIndex : switch to a view by its index in the {@link getViews Views} collection.
 *
 * TMultiView raises {@link OnActiveViewChanged OnActiveViewChanged} event
 * when its active view is changed during a postback.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TMultiView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TMultiView extends TControl
{
	const CMD_NEXTVIEW='NextView';
	const CMD_PREVIOUSVIEW='PreviousView';
	const CMD_SWITCHVIEWID='SwitchViewID';
	const CMD_SWITCHVIEWINDEX='SwitchViewIndex';
	private $_cachedActiveViewIndex=-1;
	private $_ignoreBubbleEvents=false;

	/**
	 * Processes an object that is created during parsing template.
	 * This method overrides the parent implementation by adding only {@link TView}
	 * controls as children.
	 * @param string|TComponent text string or component parsed and instantiated in template
	 * @see createdOnTemplate
	 * @throws TConfigurationException if controls other than {@link TView} is being added
	 */
	public function addParsedObject($object)
	{
		if($object instanceof TView)
			$this->getControls()->add($object);
		else if(!is_string($object))
			throw new TConfigurationException('multiview_view_required');
	}

	/**
	 * Creates a control collection object that is to be used to hold child controls
	 * @return TViewCollection control collection
	 */
	protected function createControlCollection()
	{
		return new TViewCollection($this);
	}

	/**
	 * @return integer the zero-based index of the current view in the view collection. -1 if no active view. Default is -1.
	 */
	public function getActiveViewIndex()
	{
		if($this->_cachedActiveViewIndex>-1)
			return $this->_cachedActiveViewIndex;
		else
			return $this->getControlState('ActiveViewIndex',-1);
	}

	/**
	 * @param integer the zero-based index of the current view in the view collection. -1 if no active view.
	 * @throws TInvalidDataValueException if the view index is invalid
	 */
	public function setActiveViewIndex($value)
	{
		if(($index=TPropertyValue::ensureInteger($value))<0)
			$index=-1;
		$views=$this->getViews();
		$count=$views->getCount();
		if($count===0 && $this->getControlStage()<TControl::CS_CHILD_INITIALIZED)
			$this->_cachedActiveViewIndex=$index;
		else if($index<$count)
		{
			$this->setControlState('ActiveViewIndex',$index,-1);
			$this->_cachedActiveViewIndex=-1;
			if($index>=0)
				$this->activateView($views->itemAt($index),true);
		}
		else
			throw new TInvalidDataValueException('multiview_activeviewindex_invalid',$index);
	}

	/**
	 * @return TView the currently active view, null if no active view
	 * @throws TInvalidDataValueException if the current active view index is invalid
	 */
	public function getActiveView()
	{
		$index=$this->getActiveViewIndex();
		$views=$this->getViews();
		if($index>=$views->getCount())
			throw new TInvalidDataValueException('multiview_activeviewindex_invalid',$index);
		if($index<0)
			return null;
		$view=$views->itemAt($index);
		if(!$view->getActive())
			$this->activateView($view,false);
		return $view;
	}

	/**
	 * @param TView the view to be activated
	 * @throws TInvalidOperationException if the view is not in the view collection
	 */
	public function setActiveView($view)
	{
		if(($index=$this->getViews()->indexOf($view))>=0)
			$this->setActiveViewIndex($index);
		else
			throw new TInvalidOperationException('multiview_view_inexistent');
	}

	/**
	 * Activates the specified view.
	 * If there is any view currently active, it will be deactivated.
	 * @param TView the view to be activated
	 * @param boolean whether to trigger OnActiveViewChanged event.
	 */
	protected function activateView($view,$triggerViewChangedEvent=true)
	{
		if($view->getActive())
			return;
		$triggerEvent=$triggerViewChangedEvent && ($this->getControlStage()>=TControl::CS_STATE_LOADED || ($this->getPage() && !$this->getPage()->getIsPostBack()));
		foreach($this->getViews() as $v)
		{
			if($v===$view)
			{
				$view->setActive(true);
				if($triggerEvent)
				{
					$view->onActivate(null);
					$this->onActiveViewChanged(null);
				}
			}
			else if($v->getActive())
			{
				$v->setActive(false);
				if($triggerEvent)
					$v->onDeactivate(null);
			}
		}
	}

	/**
	 * @return TViewCollection the view collection
	 */
	public function getViews()
	{
		return $this->getControls();
	}

	/**
	 * Makes the multiview ignore all bubbled events.
	 * This is method is used internally by framework and control
	 * developers.
	 */
	public function ignoreBubbleEvents()
	{
		$this->_ignoreBubbleEvents=true;
	}

	/**
	 * Initializes the active view if any.
	 * This method overrides the parent implementation.
	 * @param TEventParameter event parameter
	 */
	public function onInit($param)
	{
		parent::onInit($param);
		if($this->_cachedActiveViewIndex>=0)
			$this->setActiveViewIndex($this->_cachedActiveViewIndex);
	}

	/**
	 * Raises <b>OnActiveViewChanged</b> event.
	 * The event is raised when the currently active view is changed to a new one
	 * @param TEventParameter event parameter
	 */
	public function onActiveViewChanged($param)
	{
		$this->raiseEvent('OnActiveViewChanged',$this,$param);
	}

	/**
	 * Processes the events bubbled from child controls.
	 * The method handles view-related command events.
	 * @param TControl sender of the event
	 * @param mixed event parameter
	 * @return boolean whether this event is handled
	 */
	public function bubbleEvent($sender,$param)
	{
		if(!$this->_ignoreBubbleEvents && ($param instanceof TCommandEventParameter))
		{
			switch($param->getCommandName())
			{
				case self::CMD_NEXTVIEW:
					if(($index=$this->getActiveViewIndex())<$this->getViews()->getCount()-1)
						$this->setActiveViewIndex($index+1);
					else
						$this->setActiveViewIndex(-1);
					return true;
				case self::CMD_PREVIOUSVIEW:
					if(($index=$this->getActiveViewIndex())>=0)
						$this->setActiveViewIndex($index-1);
					return true;
				case self::CMD_SWITCHVIEWID:
					$view=$this->findControl($param->getCommandParameter());
					if($view!==null && $view->getParent()===$this)
					{
						$this->setActiveView($view);
						return true;
					}
					else
						throw new TInvalidDataValueException('multiview_viewid_invalid');
				case self::CMD_SWITCHVIEWINDEX:
					$index=TPropertyValue::ensureInteger($param->getCommandParameter());
					$this->setActiveViewIndex($index);
					return true;
			}
		}
		return false;
	}

	/**
	 * Loads state into the wizard.
	 * This method is invoked by the framework when the control state is being saved.
	 */
	public function loadState()
	{
		// a dummy call to ensure the view is activated
		$this->getActiveView();
	}

	/**
	 * Renders the currently active view.
	 * @param THtmlWriter the writer for the rendering purpose.
	 */
	public function render($writer)
	{
		if(($view=$this->getActiveView())!==null)
			$view->renderControl($writer);
	}
}

/**
 * TViewCollection class.
 * TViewCollection represents a collection that only takes {@link TView} instances
 * as collection elements.
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TMultiView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TViewCollection extends TControlCollection
{
	/**
	 * Inserts an item at the specified position.
	 * This overrides the parent implementation by ensuring only {@link TView}
	 * controls be added into the collection.
	 * @param integer the speicified position.
	 * @param mixed new item
	 * @throws TInvalidDataTypeException if the item to be inserted is neither a string nor a TControl.
	 */
	public function insertAt($index,$item)
	{
		if($item instanceof TView)
			parent::insertAt($index,$item);
		else
			throw new TInvalidDataTypeException('viewcollection_view_required');
	}
}

/**
 * TView class
 *
 * TView is a container for a group of controls. TView must be contained
 * within a {@link TMultiView} control in which only one view can be active
 * at one time.
 *
 * To activate a view, set {@link setActive Active} to true.
 * When a view is activated, it raises {@link onActivate OnActivate} event;
 * and when a view is deactivated, it raises {@link onDeactivate OnDeactivate}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TMultiView.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TView extends TControl
{
	private $_active=false;

	/**
	 * Raises <b>OnActivate</b> event.
	 * @param TEventParameter event parameter
	 */
	public function onActivate($param)
	{
		$this->raiseEvent('OnActivate',$this,$param);
	}

	/**
	 * Raises <b>OnDeactivate</b> event.
	 * @param TEventParameter event parameter
	 */
	public function onDeactivate($param)
	{
		$this->raiseEvent('OnDeactivate',$this,$param);
	}

	/**
	 * @return boolean whether this view is active. Defaults to false.
	 */
	public function getActive()
	{
		return $this->_active;
	}

	/**
	 * @param boolean whether this view is active.
	 */
	public function setActive($value)
	{
		$value=TPropertyValue::ensureBoolean($value);
		$this->_active=$value;
		parent::setVisible($value);
	}

	/**
	 * @param boolean whether the parents should also be checked if visible
	 * @return boolean whether this view is visible.
	 * The view is visible if it is active and its parent is visible.
	 */
	public function getVisible($checkParents=true)
	{
		if(($parent=$this->getParent())===null)
			return $this->getActive();
		else if($this->getActive())
			return $parent->getVisible($checkParents);
		else
			return false;
	}

	/**
	 * @param boolean
	 * @throws TInvalidOperationException whenever this method is invoked.
	 */
	public function setVisible($value)
	{
		throw new TInvalidOperationException('view_visible_readonly');
	}
}

