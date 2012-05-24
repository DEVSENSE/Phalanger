<?php
/**
 * TDraggable class file
 * 
 * @author Christophe BOULAIN (Christophe.Boulain@gmail.com)
 * @copyright Copyright &copy; 2008, PradoSoft
 * @license http://www.pradosoft.com/license
 * @package System.Web.UI.ActiveControls
 * @version $Id: TDraggable.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 */

/**
 * TDraggable is a control which can be dragged
 * 
 * This control will make "draggable" control.  
 * 
 * @author Christophe BOULAIN (Christophe.Boulain@gmail.com)
 * @copyright Copyright &copy; 2008, PradoSoft
 * @license http://www.pradosoft.com/license
 * @package System.Web.UI.ActiveControls
 * @version $Id: TDraggable.php 2773 2010-02-17 13:55:18Z Christophe.Boulain $
 */
class TDraggable extends TPanel 
{
	/**
	 * Set the handle id or css class
	 * @param string
	 */
	public function setHandle ($value)
	{
		$this->setViewState('DragHandle', TPropertyValue::ensureString($value), null);
	}
	
	/**
	 * Get the handle id or css class
	 * @return string
	 */
	public function getHandle ()
	{
		return $this->getViewState('DragHandle', null);
	}
	
	/**
	 * Determine if draggable element should revert to it orginal position
	 * upon release in an non-droppable container.
	 * @return boolean true to revert
	 */
	public function getRevert()
	{
		return $this->getViewState('Revert', true);
	}
	
	/**
	 * Sets whether the draggable element should revert to it orginal position
	 * upon release in an non-droppable container.
	 * @param boolean true to revert
	 */
	public function setRevert($value)
	{
		$this->setViewState('Revert', TPropertyValue::ensureBoolean($value), true);
	}
	
	/**
	 * Determine if the element should be cloned when dragged
	 * If true, Clones the element and drags the clone, leaving the original in place until the clone is dropped.
	 * Defaults to false
	 * @return boolean true to clone the element
	 */
	public function getGhosting ()
	{
		return $this->getViewState('Ghosting', false);
	}
	
	/**
	 * Sets wether the element should be cloned when dragged
	 * If true, Clones the element and drags the clone, leaving the original in place until the clone is dropped.
	 * Defaults to false
	 * @return boolean true to clone the element
	 */
	public function setGhosting ($value)
	{
		$this->setViewState('Ghosting', TPropertyValue::ensureBoolean($value), false);
	}
	
	/**
	 * Determine if the element should be constrainted in one direction or not
	 * @return CDraggableConstraint
	 */
	public function getConstraint()
	{
		return $this->getViewState('Constraint', TDraggableConstraint::None);
	}
	
	/**
	 * Set wether the element should be constrainted in one direction
	 * @param CDraggableConstraint
	 */
	public function setConstraint($value)
	{
		$this->setViewState('Constraint', TPropertyValue::ensureEnum($value, 'TDraggableConstraint'), TDraggableConstraint::None);
	}
	

	/**
	 * Ensure that the ID attribute is rendered and registers the javascript code
	 * for initializing the active control.
	 */
	protected function addAttributesToRender($writer)
	{
		parent::addAttributesToRender($writer);
		$writer->addAttribute('id',$this->getClientID());
		$cs=$this->getPage()->getClientScript();
		$cs->registerPradoScript('dragdrop');
		$options=TJavascript::encode($this->getPostBackOptions());
		$class=$this->getClientClassName();
		$code="new {$class}('{$this->getClientId()}', {$options}) ";
		$cs->registerEndScript(sprintf('%08X', crc32($code)), $code);
	}
		
	/**
	 * Gets the name of the javascript class responsible for performing postback for this control.
	 * This method overrides the parent implementation.
	 * @return string the javascript class name
	 */
	protected function getClientClassName ()
	{
		return 'Draggable';
	}
	
	/**
	 * Gets the post back options for this textbox.
	 * @return array
	 */
	protected function getPostBackOptions()
	{
		$options['ID'] = $this->getClientID();

		if (($handle=$this->getHandle())!== null) $options['handle']=$handle;
		$options['revert']=$this->getRevert();
		if (($constraint=$this->getConstraint())!==TDraggableConstraint::None) $options['constraint']=strtolower($constraint);
		$options['ghosting']=$this->getGhosting();

		return $options;
	}
	
}

class TDraggableConstraint extends TEnumerable
{
	const None='None';
	const Horizontal='Horizontal';
	const Vertical='Vertical';
}
?>