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
 * @subpackage Search
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */


/**
 * @package    Zend_Search_Lucene
 * @subpackage Search
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */
class Zend_Search_Lucene_Search_QueryHit
{
    /**
     * Object handle of the index
     * @var Zend_Search_Lucene
     */
    protected $_index = null;

    /**
     * Object handle of the document associated with this hit
     * @var Zend_Search_Lucene_Document
     */
    protected $_document = null;

    /**
     * Number of the document in the index
     * @var integer
     */
    public $id;

    /**
     * Score of the hit
     * @var float
     */
    public $score;


    /**
     * Constructor - pass object handle of Zend_Search_Lucene index that produced
     * the hit so the document can be retrieved easily from the hit.
     *
     * @param Zend_Search_Lucene $index
     */

    public function __construct(Zend_Search_Lucene $index)
    {
        $this->_index = $index;
    }


    /**
     * Convenience function for getting fields from the document
     * associated with this hit.
     *
     * @param string $offset
     * @return string
     */
    public function __get($offset)
    {
        return $this->getDocument()->getFieldValue($offset);
    }


    /**
     * Return the document object for this hit
     *
     * @return Zend_Search_Lucene_Document
     */
    public function getDocument()
    {
        if (!$this->_document instanceof Zend_Search_Lucene_Document) {
            $this->_document = $this->_index->getDocument($this->id);
        }

        return $this->_document;
    }


    /**
     * Return the index object for this hit
     *
     * @return Zend_Search_Lucene
     */
    public function getIndex()
    {
        return $this->_index;
    }
}

