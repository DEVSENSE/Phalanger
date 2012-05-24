<?php
/**
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id$
 * @since 3.1
 */
class FlexApp extends TTemplateControl
{
	private $_parameters;

	/**
	 * FlashVar parameter name value pairs.
	 *
	 * NOTE: parameter names must be accessed in lowercase in Flex Applications!
	 *
	 * @return TAttributeCollection
	 */
	public function getParameters()
	{
		if($this->_parameters===null)
			$this->_parameters = new TAttributeCollection();
		return $this->_parameters;
	}

	public function getFlashVars()
	{
		$params = array();
		foreach($this->getParameters() as $name=>$value)
			$params[] = $name.'='.$value;
		return implode('&', $params);
	}

	public function getWidth()
	{
		return $this->getViewState('Width', '450');
	}

	public function setWidth($value)
	{
		$this->setViewState('Width', $value, '450');
	}

	public function getHeight()
	{
		return $this->getViewState('Height', '300');
	}

	public function setHeight($value)
	{
		$this->setViewState('Height', $value, '300');
	}

	public function getBinDirectory()
	{
		return $this->getViewState('Bin');
	}

	public function setBinDirectory($value)
	{
		$this->setViewState('Bin', $value);
	}

	public function getAppName()
	{
		return $this->getViewState('AppName');
	}

	public function setAppName($value)
	{
		$this->setViewState('AppName', $value);
	}

	public function getQuality()
	{
		return $this->getViewState('Quality', 'high');
	}

	public function setQuality($value)
	{
		$this->setViewState('Quality', $value, 'high');
	}

	public function getBgcolor()
	{
		return $this->getViewState('bgcolor', '#ffffff');
	}

	public function setBgColor($value)
	{
		$this->setViewState('bgcolor', $value, '#ffffff');
	}

	public function getAlign()
	{
		return $this->getViewState('align', 'middle');
	}

	public function setAlign($value)
	{
		$this->setViewState('align', $value, 'middle');
	}

	public function getAllowScriptAccess()
	{
		return $this->getViewState('allowScriptAccess', 'sameDomain');
	}

	public function setAllowScriptAccess($value)
	{
		$this->setViewState('allowScriptAccess', $value, 'sameDomain');
	}
}

?>