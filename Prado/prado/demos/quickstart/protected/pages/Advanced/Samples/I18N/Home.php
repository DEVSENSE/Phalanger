<?php

class Home extends TPage
{
	/**
	 * Change the globalization culture using value from request "lang" parameter.
	 */
	public function __construct()
	{
		parent::__construct();
		$lang = $this->Request['lang'];
		$info = new CultureInfo();
		if($info->validCulture($lang)) //only valid lang is permitted
			$this->getApplication()->getGlobalization()->setCulture($lang);
	}

	/**
	 * Initialize the page with some arbituary data.
	 * @param TEventParameter event parameter.
	 */
	public function onLoad($param)
	{
		parent::onLoad($param);
		$time1 = $this->Time1;
		$time1->Value = time();

		$number2 = $this->Number2;
		$number2->Value = 46412.416;

		$this->dataBind();
	}

	/**
	 * Get the localized current culture name.
	 * @return string localized curreny culture name.
	 */
	public function getCurrentCulture()
	{
		$culture = $this->getApplication()->getGlobalization()->getCulture();
		$cultureInfo = new CultureInfo($culture);
		return $cultureInfo->getNativeName();
	}
}

?>