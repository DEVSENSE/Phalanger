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
 * @subpackage Analysis
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */


/** Zend_Search_Lucene_Analysis_Token */
require_once 'Zend/Search/Lucene/Analysis/Token.php';

/** Zend_Search_Lucene_Analysis_Analyzer_Common_Text */
require_once 'Zend/Search/Lucene/Analysis/Analyzer/Common/Text.php';

/** Zend_Search_Lucene_Analysis_Analyzer_Common_Text_CaseInsensitive */
require_once 'Zend/Search/Lucene/Analysis/Analyzer/Common/Text/CaseInsensitive.php';



/**
 * An Analyzer is used to analyze text.
 * It thus represents a policy for extracting index terms from text.
 *
 * Note:
 * Lucene Java implementation is oriented to streams. It provides effective work
 * with a huge documents (more then 20Mb).
 * But engine itself is not oriented such documents.
 * Thus Zend_Search_Lucene analysis API works with data strings and sets (arrays).
 *
 * @package    Zend_Search_Lucene
 * @subpackage Analysis
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */

abstract class Zend_Search_Lucene_Analysis_Analyzer
{
    /**
     * The Analyzer implementation used by default.
     *
     * @var Zend_Search_Lucene_Analysis_Analyzer
     */
    static private $_defaultImpl;

    /**
     * Tokenize text to a terms
     * Returns array of Zend_Search_Lucene_Analysis_Token objects
     *
     * @param string $data
     * @return array
     */
    abstract public function tokenize($data);


    /**
     * Set the default Analyzer implementation used by indexing code.
     *
     * @param Zend_Search_Lucene_Analysis_Analyzer $similarity
     */
    static public function setDefault(Zend_Search_Lucene_Analysis_Analyzer $analyzer)
    {
        self::$_defaultImpl = $analyzer;
    }


    /**
     * Return the default Analyzer implementation used by indexing code.
     *
     * @return Zend_Search_Lucene_Analysis_Analyzer
     */
    static public function getDefault()
    {
        if (!self::$_defaultImpl instanceof Zend_Search_Lucene_Analysis_Analyzer) {
            self::$_defaultImpl = new Zend_Search_Lucene_Analysis_Analyzer_Common_Text_CaseInsensitive();
        }

        return self::$_defaultImpl;
    }

}

