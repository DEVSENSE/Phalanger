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
class Zend_Search_Lucene_Search_Similarity_Default extends Zend_Search_Lucene_Search_Similarity
{

    /**
     * Implemented as '1/sqrt(numTerms)'.
     *
     * @param string $fieldName
     * @param integer numTerms
     * @return float
     */
    public function lengthNorm($fieldName, $numTerms)
    {
        return 1.0/sqrt($numTerms);
    }

    /**
     * Implemented as '1/sqrt(sumOfSquaredWeights)'.
     *
     * @param float $sumOfSquaredWeights
     * @return float
     */
    public function queryNorm($sumOfSquaredWeights)
    {
        return 1.0/sqrt($sumOfSquaredWeights);
    }

    /**
     * Implemented as 'sqrt(freq)'.
     *
     * @param float $freq
     * @return float
     */
    public function tf($freq)
    {
        return sqrt($freq);
    }

    /**
     * Implemented as '1/(distance + 1)'.
     *
     * @param integer $distance
     * @return float
     */
    public function sloppyFreq($distance)
    {
        return 1.0/($distance + 1);
    }

    /**
     * Implemented as 'log(numDocs/(docFreq+1)) + 1'.
     *
     * @param integer $docFreq
     * @param integer $numDocs
     * @return float
     */
    public function idfFreq($docFreq, $numDocs)
    {
        return log($numDocs/(float)($docFreq+1)) + 1.0;
    }

    /**
     * Implemented as 'overlap/maxOverlap'.
     *
     * @param integer $overlap
     * @param integer $maxOverlap
     * @return float
     */
    public function coord($overlap, $maxOverlap)
    {
        return $overlap/(float)$maxOverlap;
    }
}
