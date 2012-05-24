<?php
/**
 * TExpression class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TExpression.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 */

/**
 * TExpression class
 *
 * TExpression evaluates a PHP expression and renders the result.
 * The expression is evaluated during the rendering stage. The expression being
 * evaluated can be set via the property {@link setExpression Expression}.
 * The context of the expression evaluated is the TExpression object itself.
 *
 * Note, since TExpression allows evaluation of arbitrary PHP expression,
 * make sure {@link setExpression Expression} does not come directly from user input.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TExpression.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System.Web.UI.WebControls
 * @since 3.0
 */
class TExpression extends TControl
{
	/**
	 * @var string PHP expression to be evaluated
	 */
	private $_e='';

	/**
	 * @return string the expression to be evaluated
	 */
	public function getExpression()
	{
		return $this->_e;
	}

	/**
	 * @param string the expression to be evaluated
	 */
	public function setExpression($value)
	{
		$this->_e=$value;
	}

	/**
	 * Renders the evaluation result of the expression.
	 * @param THtmlWriter the writer used for the rendering purpose
	 */
	public function render($writer)
	{
		if($this->_e!=='')
			$writer->write($this->evaluateExpression($this->_e));
	}
}

