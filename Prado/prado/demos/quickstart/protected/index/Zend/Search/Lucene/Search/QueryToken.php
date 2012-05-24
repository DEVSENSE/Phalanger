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


/** Zend_Search_Lucene_Exception */
require_once 'Zend/Search/Lucene/Exception.php';


/**
 * @package    Zend_Search_Lucene
 * @subpackage Search
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */
class Zend_Search_Lucene_Search_QueryToken
{
    /**
     * Token type Word.
     */
    const TOKTYPE_WORD = 0;

    /**
     * Token type Field.
     * Field indicator in 'field:word' pair
     */
    const TOKTYPE_FIELD = 1;

    /**
     * Token type Sign.
     * '+' (required) or '-' (absentee) sign
     */
    const TOKTYPE_SIGN = 2;

    /**
     * Token type Bracket.
     * '(' or ')'
     */
    const TOKTYPE_BRACKET = 3;


    /**
     * Token type.
     *
     * @var integer
     */
    public $type;

    /**
     * Token text.
     *
     * @var integer
     */
    public $text;


    /**
     * IndexReader constructor needs token type and token text as a parameters.
     *
     * @param $tokType integer
     * @param $tokText string
     */
    public function __construct($tokType, $tokText)
    {
        switch ($tokType) {
            case self::TOKTYPE_BRACKET:
                // fall through to the next case
            case self::TOKTYPE_FIELD:
                // fall through to the next case
            case self::TOKTYPE_SIGN:
                // fall through to the next case
            case self::TOKTYPE_WORD:
                break;
            default:
                throw new Zend_Search_Lucene_Exception("Unrecognized token type \"$tokType\".");
        }

        if (!strlen($tokText)) {
            throw new Zend_Search_Lucene_Exception('Token text must be supplied.');
        }

        $this->type = $tokType;
        $this->text = $tokText;
    }
}

