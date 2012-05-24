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
 * @subpackage Index
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */


/** Zend_Search_Lucene_Exception */
require_once 'Zend/Search/Lucene/Exception.php';


/**
 * @package    Zend_Search_Lucene
 * @subpackage Index
 * @copyright  Copyright (c) 2005-2006 Zend Technologies USA Inc. (http://www.zend.com)
 * @license    http://www.zend.com/license/framework/1_0.txt Zend Framework License version 1.0
 */
class Zend_Search_Lucene_Index_SegmentInfo
{
    /**
     * Number of docs in a segment
     *
     * @var integer
     */
    private $_docCount;

    /**
     * Segment name
     *
     * @var string
     */
    private $_name;

    /**
     * Term Dictionary Index
     * Array of the Zend_Search_Lucene_Index_Term objects
     * Corresponding Zend_Search_Lucene_Index_TermInfo object stored in the $_termDictionaryInfos
     *
     * @var array
     */
    private $_termDictionary;

    /**
     * Term Dictionary Index TermInfos
     * Array of the Zend_Search_Lucene_Index_TermInfo objects
     *
     * @var array
     */
    private $_termDictionaryInfos;

    /**
     * Segment fields. Array of Zend_Search_Lucene_Index_FieldInfo objects for this segment
     *
     * @var array
     */
    private $_fields;

    /**
     * Field positions in a dictionary.
     * (Term dictionary contains filelds ordered by names)
     *
     * @var array
     */
    private $_fieldsDicPositions;


    /**
     * Associative array where the key is the file name and the value is data offset
     * in a compound segment file (.csf).
     *
     * @var array
     */
    private $_segFiles;

    /**
     * File system adapter.
     *
     * @var Zend_Search_Lucene_Storage_Directory_Filesystem
     */
    private $_directory;

    /**
     * Normalization factors.
     * An array fieldName => normVector
     * normVector is a binary string.
     * Each byte corresponds to an indexed document in a segment and
     * encodes normalization factor (float value, encoded by
     * Zend_Search_Lucene_Search_Similarity::encodeNorm())
     *
     * @var array
     */
    private $_norms = array();

    /**
     * Zend_Search_Lucene_Index_SegmentInfo constructor needs Segmentname,
     * Documents count and Directory as a parameter.
     *
     * @param string $name
     * @param integer $docCount
     * @param Zend_Search_Lucene_Storage_Directory $directory
     */
    public function __construct($name, $docCount, $directory)
    {
        $this->_name = $name;
        $this->_docCount = $docCount;
        $this->_directory = $directory;
        $this->_termDictionary = null;

        $this->_segFiles = array();
        $cfsFile = $this->_directory->getFileObject($name . '.cfs');
        $segFilesCount = $cfsFile->readVInt();

        for ($count = 0; $count < $segFilesCount; $count++) {
            $dataOffset = $cfsFile->readLong();
            $fileName = $cfsFile->readString();
            $this->_segFiles[$fileName] = $dataOffset;
        }

        $fnmFile = $this->openCompoundFile('.fnm');
        $fieldsCount = $fnmFile->readVInt();
        $fieldNames = array();
        $fieldNums  = array();
        $this->_fields = array();
        for ($count=0; $count < $fieldsCount; $count++) {
            $fieldName = $fnmFile->readString();
            $fieldBits = $fnmFile->readByte();
            $this->_fields[$count] = new Zend_Search_Lucene_Index_FieldInfo($fieldName,
                                                                            $fieldBits & 1,
                                                                            $count,
                                                                            $fieldBits & 2 );
            if ($fieldBits & 0x10) {
                // norms are omitted for the indexed field
                $this->_norms[$count] = str_repeat(chr(Zend_Search_Lucene_Search_Similarity::encodeNorm(1.0)), $docCount);
            }

            $fieldNums[$count]  = $count;
            $fieldNames[$count] = $fieldName;
        }
        array_multisort($fieldNames, SORT_ASC, SORT_REGULAR, $fieldNums);
        $this->_fieldsDicPositions = array_flip($fieldNums);
    }

    /**
     * Opens index file stoted within compound index file
     *
     * @param string $extension
     * @throws Zend_Search_Lucene_Exception
     * @return Zend_Search_Lucene_Storage_File
     */
    public function openCompoundFile($extension)
    {
        $filename = $this->_name . $extension;

        if( !isset($this->_segFiles[ $filename ]) ) {
            throw new Zend_Search_Lucene_Exception('Index compound file doesn\'t contain '
                                       . $filename . ' file.' );
        }

        $file = $this->_directory->getFileObject( $this->_name.".cfs" );
        $file->seek( $this->_segFiles[ $filename ] );
        return $file;
    }

    /**
     * Returns field index or -1 if field is not found
     *
     * @param string $fieldName
     * @return integer
     */
    public function getFieldNum($fieldName)
    {
        foreach( $this->_fields as $field ) {
            if( $field->name == $fieldName ) {
                return $field->number;
            }
        }

        return -1;
    }

    /**
     * Returns field info for specified field
     *
     * @param integer $fieldNum
     * @return ZSearchFieldInfo
     */
    public function getField($fieldNum)
    {
        return $this->_fields[$fieldNum];
    }

    /**
     * Returns array of fields.
     * if $indexed parameter is true, then returns only indexed fields.
     *
     * @param boolean $indexed
     * @return array
     */
    public function getFields($indexed = false)
    {
        $result = array();
        foreach( $this->_fields as $field ) {
            if( (!$indexed) || $field->isIndexed ) {
                $result[ $field->name ] = $field->name;
            }
        }
        return $result;
    }

    /**
     * Returns the total number of documents in this segment.
     *
     * @return integer
     */
    public function count()
    {
        return $this->_docCount;
    }


    /**
     * Loads Term dictionary from TermInfoIndex file
     */
    protected function _loadDictionary()
    {
        if ($this->_termDictionary !== null) {
            return;
        }

        $this->_termDictionary = array();
        $this->_termDictionaryInfos = array();

        $tiiFile = $this->openCompoundFile('.tii');
        $tiVersion = $tiiFile->readInt();
        if ($tiVersion != (int)0xFFFFFFFE) {
            throw new Zend_Search_Lucene_Exception('Wrong TermInfoIndexFile file format');
        }

        $indexTermCount = $tiiFile->readLong();
                          $tiiFile->readInt();  // IndexInterval
        $skipInterval   = $tiiFile->readInt();

        $prevTerm     = '';
        $freqPointer  =  0;
        $proxPointer  =  0;
        $indexPointer =  0;
        for ($count = 0; $count < $indexTermCount; $count++) {
            $termPrefixLength = $tiiFile->readVInt();
            $termSuffix       = $tiiFile->readString();
            $termValue        = substr( $prevTerm, 0, $termPrefixLength ) . $termSuffix;

            $termFieldNum     = $tiiFile->readVInt();
            $docFreq          = $tiiFile->readVInt();
            $freqPointer     += $tiiFile->readVInt();
            $proxPointer     += $tiiFile->readVInt();
            if( $docFreq >= $skipInterval ) {
                $skipDelta = $tiiFile->readVInt();
            } else {
                $skipDelta = 0;
            }

            $indexPointer += $tiiFile->readVInt();

            $this->_termDictionary[] =  new Zend_Search_Lucene_Index_Term($termValue,$termFieldNum);
            $this->_termDictionaryInfos[] =
                new Zend_Search_Lucene_Index_TermInfo($docFreq, $freqPointer, $proxPointer, $skipDelta, $indexPointer);
            $prevTerm = $termValue;
        }
    }


    /**
     * Return segment name
     *
     * @return string
     */
    public function getName()
    {
        return $this->_name;
    }


    /**
     * Scans terms dictionary and returns term info
     *
     * @param Zend_Search_Lucene_Index_Term $term
     * @return Zend_Search_Lucene_Index_TermInfo
     */
    public function getTermInfo($term)
    {
        $this->_loadDictionary();

        $searchField = $this->getFieldNum($term->field);

        if ($searchField == -1) {
            return null;
        }
        $searchDicField = $this->_fieldsDicPositions[$searchField];

        // search for appropriate value in dictionary
        $lowIndex = 0;
        $highIndex = count($this->_termDictionary)-1;
        while ($highIndex >= $lowIndex) {
            // $mid = ($highIndex - $lowIndex)/2;
            $mid = ($highIndex + $lowIndex) >> 1;
            $midTerm = $this->_termDictionary[$mid];

            $delta = $searchDicField - $this->_fieldsDicPositions[$midTerm->field];
            if ($delta == 0) {
                $delta = strcmp($term->text, $midTerm->text);
            }

            if ($delta < 0) {
                $highIndex = $mid-1;
            } elseif ($delta > 0) {
                $lowIndex  = $mid+1;
            } else {
                return $this->_termDictionaryInfos[$mid]; // We got it!
            }
        }

        if ($highIndex == -1) {
            // Term is out of the dictionary range
            return null;
        }

        $prevPosition = $highIndex;
        $prevTerm = $this->_termDictionary[$prevPosition];
        $prevTermInfo = $this->_termDictionaryInfos[ $prevPosition ];

        $tisFile = $this->openCompoundFile('.tis');
        $tiVersion = $tisFile->readInt();
        if ($tiVersion != (int)0xFFFFFFFE) {
            throw new Zend_Search_Lucene_Exception('Wrong TermInfoFile file format');
        }

        $termCount     = $tisFile->readLong();
        $indexInterval = $tisFile->readInt();
        $skipInterval  = $tisFile->readInt();

        $tisFile->seek($prevTermInfo->indexPointer - 20 /* header size*/, SEEK_CUR);

        $termValue    = $prevTerm->text;
        $termFieldNum = $prevTerm->field;
        $freqPointer = $prevTermInfo->freqPointer;
        $proxPointer = $prevTermInfo->proxPointer;
        for ($count = $prevPosition*$indexInterval + 1;
             $count < $termCount &&
             ( $this->_fieldsDicPositions[ $termFieldNum ] < $searchDicField ||
              ($this->_fieldsDicPositions[ $termFieldNum ] == $searchDicField &&
               strcmp($termValue, $term->text) < 0) );
             $count++) {
            $termPrefixLength = $tisFile->readVInt();
            $termSuffix       = $tisFile->readString();
            $termFieldNum     = $tisFile->readVInt();
            $termValue        = substr( $termValue, 0, $termPrefixLength ) . $termSuffix;

            $docFreq      = $tisFile->readVInt();
            $freqPointer += $tisFile->readVInt();
            $proxPointer += $tisFile->readVInt();
            if( $docFreq >= $skipInterval ) {
                $skipOffset = $tisFile->readVInt();
            } else {
                $skipOffset = 0;
            }
        }

        if ($termFieldNum == $searchField && $termValue == $term->text) {
            return new Zend_Search_Lucene_Index_TermInfo($docFreq, $freqPointer, $proxPointer, $skipOffset);
        } else {
            return null;
        }
    }

    /**
     * Returns normalization factor for specified documents
     *
     * @param integer $id
     * @param string $fieldName
     * @return string
     */
    public function norm($id, $fieldName)
    {
        $fieldNum = $this->getFieldNum($fieldName);

        if ( !($this->_fields[$fieldNum]->isIndexed) ) {
            return null;
        }

        if ( !isset( $this->_norms[$fieldNum] )) {
            $fFile = $this->openCompoundFile('.f' . $fieldNum);
            $this->_norms[$fieldNum] = $fFile->readBytes($this->_docCount);
        }

        return Zend_Search_Lucene_Search_Similarity::decodeNorm( ord($this->_norms[$fieldNum]{$id}) );
    }
}

