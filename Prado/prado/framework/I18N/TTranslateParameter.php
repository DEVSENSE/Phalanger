<?php
/**
 * TTranslateParameter component.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TTranslateParameter.php 2919 2011-05-21 18:14:36Z ctrlaltca@gmail.com $
 * @package System.I18N
 */

/**
 * TTranslateParameter component should be used inside the TTranslate component to
 * allow parameter substitution.
 *
 * For example, the strings "{greeting}" and "{name}" will be replace
 * with the values of "Hello" and "World", respectively.
 * The substitution string must be enclose with "{" and "}".
 * The parameters can be further translated by using TTranslate.
 * <code>
 * <com:TTranslate>
 *   {greeting} {name}!
 *   <com:TTranslateParameter Key="name">World</com:TTranslateParameter>
 *   <com:TTranslateParameter Key="greeting">Hello</com:TTranslateParameter>
 * </com:TTranslate>
 * </code>
 *
 * Namespace: System.I18N
 *
 * Properties
 * - <b>Key</b>, string, <b>required</b>.
 *   <br>Gets or sets the string in TTranslate to substitute.
 * - <b>Trim</b>, boolean,
 *   <br>Gets or sets an option to trim the contents of the TParam.
 *   Default is to trim the contents.
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version v3.0, last update on Friday, 6 January 2006
 * @package System.I18N
 */
class TTranslateParameter extends TControl
{
	/**
	 * The substitution key.
	 * @var string
	 */
	protected $key;

	/**
	 * To trim or not to trim the contents.
	 * @var boolean
	 */
	protected $trim = true;


	/**
	 * Get the parameter substitution key.
	 * @return string substitution key.
	 */
	public function getKey()
	{
		if(empty($this->key))
			throw new TException('The Key property must be specified.');
		return $this->key;
	}

	/**
	 * Set the parameter substitution key.
	 * @param string substitution key.
	 */
	public function setKey($value)
	{
		$this->key = $value;
	}

	/**
	 * Set the option to trim the contents.
	 * @param boolean trim or not.
	 */
	public function setTrim($value)
	{
		$this->trim = TPropertyValue::ensureBoolean($value);
	}

	/**
	 * Trim the content or not.
	 * @return boolean trim or not.
	 */
	public function getTrim()
	{
		return $this->trim;
	}

	public function getValue()
	{
		return $this->getViewState('Value', '');
	}

	public function setValue($value)
	{
		$this->setViewState('Value', $value, '');
	}

	/**
	 * @return string parameter contents.
	 */
	public function getParameter()
	{
		$value = $this->getValue();
		if(strlen($value) > 0)
			return $value;
		$htmlWriter = Prado::createComponent($this->GetResponse()->getHtmlWriterType(), new TTextWriter());
		$this->renderControl($htmlWriter);
		return $this->getTrim() ?
			trim($htmlWriter->flush()) : $htmlWriter->flush();
	}
}

