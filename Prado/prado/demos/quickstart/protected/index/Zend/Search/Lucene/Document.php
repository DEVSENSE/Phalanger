<?php
/**
 * Zend Framework
 *
 * LICENSE
 *
 * This source file is subject to version 1.0 of the Zend Framework
 * license, that is bundled with this package in the file LICENSE, and
 * is available through the world-wide-web at the following URL:
 * http://www.zend.com/license/framework/1_0.txt. If you did not receive
 * a copy of the Zend Framework license and are unable to obtain it
 * through the world-wide-web, please send a note to license@zend.com
 * so we can mail you a copy immediately.
 *
 * @package    Zend_Search_Lucene
 * @subpackage document
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */


/** Zend_Search_Lucene_Field */
require_once 'Zend/Search/Lucene/Field.php';


/**
 * A Document is a set of fields. Each field has a name and a textual value.
 *
 * @package    Zend_Search_Lucene
 * @subpackage document
 * @copyright  Copyright (c) 2005-2006 Zend Technologies Inc. (http://www.zend.com)
 * @license    Zend Framework License version 1.0
 */
class Zend_Search_Lucene_Document
{

    /**
     * Associative array Zend_Search_Lucene_Field objects where the keys to the
     * array are the names of the fields.
     *
     * @var array
     */
    protected $_fields = array();

    public $boost = 1.0;


    /**
     * Proxy method for getFieldValue(), provides more convenient access to
     * the string value of a field.
     *
     * @param  $offset
     * @return string
     */
	public function __get($offset)
	{
		return $this->getFieldValue($offset);
	}


    /**
     * Add a field object to this document.
     *
     * @param Zend_Search_Lucene_Field $field
     */
    public function addField(Zend_Search_Lucene_Field $field)
    {
        $this->_fields[$field->name] = $field;
    }


    /**
     * Return an array with the names of the fields in this document.
     *
     * @return array
     */
    public function getFieldNames()
    {
    	return array_keys($this->_fields);
    }


    /**
     * Returns Zend_Search_Lucene_Field object for a named field in this document.
     *
     * @param string $fieldName
     * @return Zend_Search_Lucene_Field
     */
    public function getField($fieldName)
    {
		if (!array_key_exists($fieldName, $this->_fields)) {
			throw new Zend_Search_Lucene_Exception("Field name \"$fieldName\" not found in document.");
		}
        return $this->_fields[$fieldName];
    }


    /**
     * Returns the string value of a named field in this document.
     *
     * @see __get()
     * @return string
     */
    public function getFieldValue($fieldName)
    {
    	return $this->getField($fieldName)->stringValue;
    }

}
