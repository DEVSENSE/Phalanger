<?php
/**
 * WsdlOperation file.
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
 * @version $Id: WsdlOperation.php 1689 2007-02-12 12:46:11Z wei $
 * @package System.Web.Services.SOAP
 */

/**
 * Represents a WSDL Operation. This is exported for the portTypes and bindings
 * section of the soap service
 * @author 		Marcus Nyeholt		<tanus@users.sourceforge.net>
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version 	$Revision$
 */
class WsdlOperation
{
	/**
	 * The name of the operation
	 */
	private $operationName;

	/**
	 * Documentation for the operation
	 */
	private $documentation;

	/**
	 * The input wsdl message
	 */
	private $inputMessage;

	/**
	 * The output wsdl message
	 */
	private $outputMessage;

	public function __construct($name, $doc='')
	{
		$this->operationName = $name;
		$this->documentation = $doc;
	}

	public function setInputMessage(WsdlMessage $msg)
	{
		$this->inputMessage = $msg;
	}

	public function setOutputMessage(WsdlMessage $msg)
	{
		$this->outputMessage = $msg;
	}

	/**
	 * Sets the message elements for this operation into the wsdl document
	 * @param 	DOMElement 		$wsdl		The parent domelement for the messages
	 * @param 	DomDocument		$dom		The dom document to create the messages as children of
	 */
	public function setMessageElements(DOMElement $wsdl, DOMDocument $dom)
	{

		$input = $this->inputMessage->getMessageElement($dom);
		$output = $this->outputMessage->getMessageElement($dom);

		$wsdl->appendChild($input);
		$wsdl->appendChild($output);
	}

	/**
	 * Get the port operations for this operation
	 * @param 	DomDocument		$dom		The dom document to create the messages as children of
	 * @return 	DomElement					The dom element representing this port.
	 */
	public function getPortOperation(DomDocument $dom)
	{
		$operation = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:operation');
		$operation->setAttribute('name', $this->operationName);

		$documentation = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:documentation', htmlentities($this->documentation));
		$input = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:input');
		$input->setAttribute('message', 'tns:'.$this->inputMessage->getName());
		$output = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:output');
		$output->setAttribute('message', 'tns:'.$this->outputMessage->getName());

		$operation->appendChild($documentation);
		$operation->appendChild($input);
		$operation->appendChild($output);

		return $operation;
	}

	/**
	 * Build the binding operations.
	 * TODO: Still quite incomplete with all the things being stuck in, I don't understand
	 * a lot of it, and it's mostly copied from the output of nusoap's wsdl output.
	 * @param 	DomDocument		$dom		The dom document to create the binding as children of
	 * @param 	string			$namespace	The namespace this binding is in.
	 * @return 	DomElement					The dom element representing this binding.
	 */
	public function getBindingOperation(DomDocument $dom, $namespace, $style='rpc')
	{
		$operation = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:operation');
		$operation->setAttribute('name', $this->operationName);

		$soapOperation = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/soap/', 'soap:operation');
		$method = $this->operationName;
		$soapOperation->setAttribute('soapAction', $namespace.'#'.$method);
		$soapOperation->setAttribute('style', $style);

		$input = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:input');
		$output = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/', 'wsdl:output');

		$soapBody = $dom->createElementNS('http://schemas.xmlsoap.org/wsdl/soap/', 'soap:body');
		$soapBody->setAttribute('use', 'encoded');
		$soapBody->setAttribute('namespace', $namespace);
		$soapBody->setAttribute('encodingStyle', 'http://schemas.xmlsoap.org/soap/encoding/');
		$input->appendChild($soapBody);
		$output->appendChild(clone $soapBody);

		$operation->appendChild($soapOperation);
		$operation->appendChild($input);
		$operation->appendChild($output);

		return $operation;
	}
}
?>