<?php
/**
 * WsdlMessage file.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the BSD License.
 *
 * Copyright(c) 2005 by Marcus Nyeholt. All rights reserved.
 *
 * To contact the author write to {@link mailto:tanus@users.sourceforge.net Marcus Nyeholt}
 * This file is part of the PRADO framework from {@link http://www.xisc.com}
 *
 * @author Marcus Nyeholt		<tanus@users.sourceforge.net>
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: WsdlMessage.php 1689 2007-02-12 12:46:11Z wei $
 * @package System.Web.Services.SOAP
 */

/**
 * Represents a WSDL message. This is bound to the portTypes
 * for this service
 * @author 		Marcus Nyeholt		<tanus@users.sourceforge.net>
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version 	$Revision$
 */
class WsdlMessage
{
	/**
	 * The name of this message
	 * @var 	string
	 */
	private $name;

	/**
	 * Represents the parameters for this message
	 * @var 	array
	 */
	private $parts;

	/**
	 * Creates a new message
	 * @param 	string		$messageName	The name of the message
	 * @param 	string		$parts			The parts of this message
	 */
	public function __construct($messageName, $parts)
	{
		$this->name = $messageName;
		$this->parts = $parts;

	}

	/**
	 * Gets the name of this message
	 * @return 		string		The name
	 */
	public function getName()
	{
		return $this->name;
	}

	/**
	 * Return the message as a DOM element
	 * @param 		DOMDocument		$wsdl		The wsdl document the messages will be children of
	 */
	public function getMessageElement(DOMDocument $dom)
	{
		$message = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:message');
		$message->setAttribute('name', $this->name);

		foreach ($this->parts as $part) {
			if (isset($part['name'])) {
				$partElement = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:part');
				$partElement->setAttribute('name', $part['name']);
				$partElement->setAttribute('type', $part['type']);
				$message->appendChild($partElement);
			}
		}

		return $message;
	}
}
?>