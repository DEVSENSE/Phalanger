<?php
/**
 * WsdlGenerator file.
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
 * @version $Id: WsdlGenerator.php 2393 2008-03-07 17:12:19Z xue $
 * @package System.Web.Services.SOAP
 */

require_once(dirname(__FILE__).'/Wsdl.php');
require_once(dirname(__FILE__).'/WsdlMessage.php');
require_once(dirname(__FILE__).'/WsdlOperation.php');

/**
 * Generator for the wsdl.
 * Special thanks to Cristian Losada for implementing the Complex Types section of the WSDL.
 * @author 		Marcus Nyeholt		<tanus@users.sourceforge.net>
 * @author 		Cristian Losada		<cristian@teaxul.com>
 * @version 	$Revision$
 */
class WsdlGenerator
{
	/**
	 * The instance.
	 * var		WsdlGenerator
	 */
	private static $instance;

	/**
	 * The name of this service (the classname)
	 * @var 	string
	 */
	private $serviceName = '';

	/**
	 * The complex types to use in the wsdl
	 * @var 	Array
	 */
	private $types = array();

	/**
	 * The operations available in this wsdl
	 * @var 	ArrayObject
	 */
	private $operations;

	/**
	 * The wsdl object.
	 * @var 	object
	 */
	private $wsdlDocument;

	/**
	 * The actual wsdl string
	 * @var 	string
	 */
	private $wsdl = '';

	/**
	 * The singleton instance for the generator
	 */
	public static function getInstance()
	{
		if (is_null(self::$instance)) {
			self::$instance = new WsdlGenerator();
		}
		return self::$instance;
	}

	/**
	 * Get the Wsdl generated
	 * @return 	string		The Wsdl for this wsdl
	 */
	public function getWsdl()
	{
		return $this->wsdl;
	}

	/**
	 * Generates WSDL for a passed in class, and saves it in the current object. The
	 * WSDL can then be retrieved by calling
	 * @param 	string		$className		The name of the class to generate for
	 * @param 	string		$serviceUri		The URI of the service that handles this WSDL
	 * @param string $encoding character encoding.
	 * @return 	void
	 */
	public function generateWsdl($className, $serviceUri='',$encoding='')
	{
		$this->wsdlDocument = new Wsdl($className, $serviceUri, $encoding);

		$classReflect = new ReflectionClass($className);
		$methods = $classReflect->getMethods();

		foreach ($methods as $method) {
			// Only process public methods
			if ($method->isPublic()) {
				$this->processMethod($method);
			}
		}

		foreach($this->types as $type => $elements) {
			$this->wsdlDocument->addComplexType($type, $elements);
		}

		$this->wsdl = $this->wsdlDocument->getWsdl();
	}

	/**
	 * Static method that generates and outputs the generated wsdl
	 * @param 		string		$className		The name of the class to export
	 * @param 		string		$serviceUri		The URI of the service that handles this WSDL
	 * @param string $encoding character encoding.
	 */
	public static function generate($className, $serviceUri='', $encoding='')
	{
		$generator = WsdlGenerator::getInstance();
		$generator->generateWsdl($className, $serviceUri,$encoding);
		//header('Content-type: text/xml');
		return $generator->getWsdl();
		//exit();

	}

	/**
	 * Process a method found in the passed in class.
	 * @param 		ReflectionMethod		$method		The method to process
	 */
	protected function processMethod(ReflectionMethod $method)
	{
		$comment = $method->getDocComment();
		if (strpos($comment, '@soapmethod') === false) {
			return;
		}
		$comment = preg_replace("/(^[\\s]*\\/\\*\\*)
                                 |(^[\\s]\\*\\/)
                                 |(^[\\s]*\\*?\\s)
                                 |(^[\\s]*)
                                 |(^[\\t]*)/ixm", "", $comment);

	    $comment = str_replace("\r", "", $comment);
	    $comment = preg_replace("/([\\t])+/", "\t", $comment);
	    $commentLines = explode("\n", $comment);

		$methodDoc = '';
		$params = array();
		$return = array();
		$gotDesc = false;
		$gotParams = false;

		foreach ($commentLines as $line) {
			if ($line == '') continue;
			if ($line{0} == '@') {
				$gotDesc = true;
				if (preg_match('/^@param\s+([\w\[\]()]+)\s+\$([\w()]+)\s*(.*)/i', $line, $match)) {
					$param = array();
					$param['type'] = $this->convertType($match[1]);
					$param['name'] = $match[2];
					$param['desc'] = $match[3];
					$params[] = $param;
				}
				else if (preg_match('/^@return\s+([\w\[\]()]+)\s*(.*)/i', $line, $match)) {
					$gotParams = true;
					$return['type'] = $this->convertType($match[1]);
					$return['desc'] = $match[2];
					$return['name'] = 'return';
				}
			}
			else {
				if (!$gotDesc) {
					$methodDoc .= trim($line);
				}
				else if (!$gotParams) {
					$params[count($params)-1]['desc'] .= trim($line);
				}
				else {
					if ($line == '*/') continue;
					$return['desc'] .= trim($line);
				}
			}
		}

		$methodName = $method->getName();
		$operation = new WsdlOperation($methodName, $methodDoc);

		$operation->setInputMessage(new WsdlMessage($methodName.'Request', $params));
		$operation->setOutputMessage(new WsdlMessage($methodName.'Response', array($return)));

		$this->wsdlDocument->addOperation($operation);

	}

	/**
	 * Converts from a PHP type into a WSDL type. This is borrowed from
	 * Cerebral Cortex (let me know and I'll remove asap).
	 *
	 * TODO: date and dateTime
	 * @param 		string		$type		The php type to convert
	 * @return 		string					The XSD type.
	 */
	private function convertType($type)
	{
		 switch ($type) {
             case 'string':
             case 'str':
                 return 'xsd:string';
                 break;
             case 'int':
             case 'integer':
                 return 'xsd:int';
                 break;
             case 'float':
             case 'double':
                 return 'xsd:float';
                 break;
             case 'boolean':
             case 'bool':
                 return 'xsd:boolean';
                 break;
             case 'date':
                 return 'xsd:date';
                 break;
             case 'time':
                 return 'xsd:time';
                 break;
             case 'dateTime':
                 return 'xsd:dateTime';
                 break;
             case 'array':
                 return 'soap-enc:Array';
                 break;
             case 'object':
                 return 'xsd:struct';
                 break;
             case 'mixed':
                 return 'xsd:anyType';
                 break;
             case 'void':
                 return '';
             default:
             	 if(strpos($type, '[]'))  // if it is an array
             	 {
             	 	$className = substr($type, 0, strlen($type) - 2);
             	 	$type = $className . 'Array';
             	 	$this->types[$type] = '';
             	 	$this->convertType($className);
             	 }
             	 else
             	 {
             	 	 if(!isset($this->types[$type]))
	             		$this->extractClassProperties($type);
             	 }
                 return 'tns:' . $type;
         }
	}

	/**
	 * Extract the type and the name of all properties of the $className class and saves it in the $types array
	 * This method extract properties from PHPDoc formatted comments for variables. Unfortunately the reflectionproperty
	 * class doesn't have a getDocComment method to extract comments about it, so we have to extract the information
	 * about the variables manually. Thanks heaps to Cristian Losada for implementing this.
	 * @param string $className The name of the class
	 */
	private function extractClassProperties($className)
	{
		/**
		 * modified by Qiang Xue, Jan. 2, 2007
		 * Using Reflection's DocComment to obtain property definitions
		 * DocComment is available since PHP 5.1
		 */
		$reflection = new ReflectionClass($className);
		$properties = $reflection->getProperties();
		foreach($properties as $property)
		{
			$comment = $property->getDocComment();
			if(strpos($comment, '@soapproperty') !== false)
			{
				if (preg_match('/@var\s+(\w+(\[\])?)\s+\$(\w+)/mi', $comment, $match)) {
					$param = array();
					$param['type'] = $this->convertType($match[1]);
					$param['name'] = $match[3];
					$this->types[$className][] = $param;
				}
			}
		}
	}
}
?>