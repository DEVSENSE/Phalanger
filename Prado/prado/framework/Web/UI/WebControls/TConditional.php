<?php
/**
 * TConditional class file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TConditional.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TConditional class.
 *
 * TConditional displays appropriate content based on the evaluation result
 * of a PHP expression specified via {@link setCondition Condition}.
 * If the result is true, it instantiates the template {@link getTrueTemplate TrueTemplate};
 * otherwise, the template {@link getFalseTemplate FalseTemplate} is instantiated.
 * The PHP expression is evaluated right before {@link onInit} stage of the control lifecycle.
 *
 * Since {@link setCondition Condition} is evaluated at a very early stage, it is recommended
 * you set {@link setCondition Condition} in template and the expression should not refer to
 * objects that are available on or after {@link onInit} lifecycle.
 *
 * A typical usage of TConditional is shown as following:
 * <code>
 * <com:TConditional Condition="$this->User->IsGuest">
 *   <prop:TrueTemplate>
 *     <a href="path/to/login">Login</a>
 *   </prop:TrueTemplate>
 *   <prop:FalseTemplate>
 *     <a href="path/to/logout">Logout</a>
 *   </prop:FalseTemplate>
 * </com:TConditional>
 * </code>
 *
 * TConditional is very light. It instantiates either {@link getTrueTemplate TrueTemplate}
 * or {@link getFalseTemplate FalseTemplate}, but never both. And the condition is evaluated only once.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TConditional.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.1.1
 */
class TConditional extends TControl
{
	private $_condition='true';
	private $_trueTemplate;
	private $_falseTemplate;
	private $_creatingChildren=false;

	/**
	 * Processes an object that is created during parsing template.
	 * This method overrides the parent implementation by removing
	 * all contents enclosed in the template tag.
	 * @param string|TComponent text string or component parsed and instantiated in template
	 * @see createdOnTemplate
	 */
	public function addParsedObject($object)
	{
		if($this->_creatingChildren)
			parent::addParsedObject($object);
	}

	/**
	 * Creates child controls.
	 * This method overrides the parent implementation. It evaluates {@link getCondition Condition}
	 * and instantiate the corresponding template.
	 */
	public function createChildControls()
	{
		$this->_creatingChildren=true;
		$result=true;
		try
		{
			$result=$this->getTemplateControl()->evaluateExpression($this->_condition);
		}
		catch(Exception $e)
		{
			throw new TInvalidDataValueException('conditional_condition_invalid',$this->_condition,$e->getMessage());
		}
		if($result)
		{
			if($this->_trueTemplate)
				$this->_trueTemplate->instantiateIn($this->getTemplateControl(),$this);
		}
		else if($this->_falseTemplate)
			$this->_falseTemplate->instantiateIn($this->getTemplateControl(),$this);
		$this->_creatingChildren=false;
	}

	/**
	 * @return string the PHP expression used for determining which template to use. Defaults to 'true', meaning using TrueTemplate.
	 */
	public function getCondition()
	{
		return $this->_condition;
	}

	/**
	 * Sets the PHP expression to be evaluated for conditionally displaying content.
	 * The context of the expression is the template control containing TConditional.
	 * @param string the PHP expression used for determining which template to use.
	 */
	public function setCondition($value)
	{
		$this->_condition=TPropertyValue::ensureString($value);
	}

	/**
	 * @return ITemplate the template applied when {@link getCondition Condition} is true.
	 */
	public function getTrueTemplate()
	{
		return $this->_trueTemplate;
	}

	/**
	 * @param ITemplate the template applied when {@link getCondition Condition} is true.
	 */
	public function setTrueTemplate(ITemplate $value)
	{
		$this->_trueTemplate=$value;
	}

	/**
	 * @return ITemplate the template applied when {@link getCondition Condition} is false.
	 */
	public function getFalseTemplate()
	{
		return $this->_falseTemplate;
	}

	/**
	 * @param ITemplate the template applied when {@link getCondition Condition} is false.
	 */
	public function setFalseTemplate(ITemplate $value)
	{
		$this->_falseTemplate=$value;
	}
}

