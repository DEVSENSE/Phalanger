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


/** Zend_Search_Lucene_Analysis_Analyzer_Common */
require_once 'Zend/Search/Lucene/Analysis/Analyzer/Common.php';


/**
 * @package    Zend_Search_Lucene
 * @subpackage Analysis
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */

class Zend_Search_Lucene_Analysis_Analyzer_Common_Text extends Zend_Search_Lucene_Analysis_Analyzer_Common
{
    /**
     * Tokenize text to a terms
     * Returns array of Zend_Search_Lucene_Analysis_Token objects
     *
     * @param string $data
     * @return array
     */
    public function tokenize($data)
    {
        $tokenStream = array();

        $position = 0;
        while ($position < strlen($data)) {
            // skip white space
            while ($position < strlen($data) && !ctype_alpha( $data{$position} )) {
                $position++;
            }

            $termStartPosition = $position;

            // read token
            while ($position < strlen($data) && ctype_alpha( $data{$position} )) {
                $position++;
            }

            // Empty token, end of stream.
            if ($position == $termStartPosition) {
                break;
            }

            $token = new Zend_Search_Lucene_Analysis_Token(substr($data,
                                             $termStartPosition,
                                             $position-$termStartPosition),
                                      $termStartPosition,
                                      $position);
            $tokenStream[] = $this->normalize($token);
        }

        return $tokenStream;
    }
}

