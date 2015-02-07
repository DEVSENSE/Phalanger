using System;
using PHP.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Library.SPL
{
    //[ImplementsType]
    //public class SplFileInfo : PhpObject
    //{
        //__construct ( ScriptContext/*!*/context , string $file_name )
        //public int getATime ( ScriptContext/*!*/context )
        //public string getBasename ( ScriptContext/*!*/context [ string $suffix ] )
        //public int getCTime ( ScriptContext/*!*/context )
        //public string getExtension ( ScriptContext/*!*/context )
        //public SplFileInfo getFileInfo ( ScriptContext/*!*/context [ string $class_name ] )
        //public string getFilename ( ScriptContext/*!*/context )
        //public int getGroup ( ScriptContext/*!*/context )
        //public int getInode ( ScriptContext/*!*/context )
        //public string getLinkTarget ( ScriptContext/*!*/context )
        //public int getMTime ( ScriptContext/*!*/context )
        //public int getOwner ( ScriptContext/*!*/context )
        //public string getPath ( ScriptContext/*!*/context )
        //public SplFileInfo getPathInfo ( ScriptContext/*!*/context [ string $class_name ] )
        //public string getPathname ( ScriptContext/*!*/context )
        //public int getPerms ( ScriptContext/*!*/context )
        //public string getRealPath ( ScriptContext/*!*/context )
        //public int getSize ( ScriptContext/*!*/context )
        //public string getType ( ScriptContext/*!*/context )
        //public bool isDir ( ScriptContext/*!*/context )
        //public bool isExecutable ( ScriptContext/*!*/context )
        //public bool isFile ( ScriptContext/*!*/context )
        //public bool isLink ( ScriptContext/*!*/context )
        //public bool isReadable ( ScriptContext/*!*/context )
        //public bool isWritable ( ScriptContext/*!*/context )
        //public /*SplFileObject*/object openFile ( ScriptContext/*!*/context [ string $open_mode = r [, bool $use_include_path = false [, resource $context = NULL ]]] )
        //public void setFileClass ( ScriptContext/*!*/context [ string $class_name ] )
        //public void setInfoClass ( ScriptContext/*!*/context [ string $class_name ] )
        //public void __toString ( ScriptContext/*!*/context )
    //}

//    [ImplementsType]
//    class SplFileObject : SplFileInfo, /*RecursiveIterator ,*/ Traversable , Iterator , SeekableIterator
//    {
        //// Constants
        //const int DROP_NEW_LINE = 1 ;
        //const int READ_AHEAD = 2 ;
        //const int SKIP_EMPTY = 6 ;
        //const int READ_CSV = 8 ;
        //// Methods
        //__construct (ScriptContext/*!*/context , string $filename [, string $open_mode = "r" [, bool $use_include_path = false [, resource $context ]]] )
        //public string|array current (ScriptContext/*!*/context )
        //public bool eof (ScriptContext/*!*/context )
        //public bool fflush (ScriptContext/*!*/context )
        //public string fgetc (ScriptContext/*!*/context )
        //public array fgetcsv (ScriptContext/*!*/context [ string $delimiter = "," [, string $enclosure = "\"" [, string $escape = "\\" ]]] )
        //public string fgets (ScriptContext/*!*/context )
        //public string fgetss (ScriptContext/*!*/context [ string $allowable_tags ] )
        //public bool flock (ScriptContext/*!*/context , int $operation [, int &$wouldblock ] )
        //public int fpassthru (ScriptContext/*!*/context )
        //public int fputcsv (ScriptContext/*!*/context , string $fields [, string $delimiter [, string $enclosure ]] )
        //public mixed fscanf (ScriptContext/*!*/context , string $format [, mixed &$... ] )
        //public int fseek (ScriptContext/*!*/context , int $offset [, int $whence = SEEK_SET ] )
        //public array fstat (ScriptContext/*!*/context )
        //public int ftell (ScriptContext/*!*/context )
        //public bool ftruncate (ScriptContext/*!*/context , int $size )
        //public int fwrite (ScriptContext/*!*/context , string $str [, int $length ] )
        //public void getChildren (ScriptContext/*!*/context )
        //public array getCsvControl (ScriptContext/*!*/context )
        //public int getFlags (ScriptContext/*!*/context )
        //public int getMaxLineLen (ScriptContext/*!*/context )
        //public bool hasChildren (ScriptContext/*!*/context )
        //public int key (ScriptContext/*!*/context )
        //public void next (ScriptContext/*!*/context )
        //public void rewind (ScriptContext/*!*/context )
        //public void seek (ScriptContext/*!*/context , int $line_pos )
        //public void setCsvControl (ScriptContext/*!*/context [ string $delimiter = "," [, string $enclosure = "\"" [, string $escape = "\\" ]]] )
        //public void setFlags (ScriptContext/*!*/context , int $flags )
        //public void setMaxLineLen (ScriptContext/*!*/context , int $max_len )
        //public bool valid (ScriptContext/*!*/context )
        //// Inherited methods
        //SplFileInfo::__construct (ScriptContext/*!*/context , string $file_name )
        //public int SplFileInfo::getATime (ScriptContext/*!*/context )
        //public string SplFileInfo::getBasename (ScriptContext/*!*/context [ string $suffix ] )
        //public int SplFileInfo::getCTime (ScriptContext/*!*/context )
        //public string SplFileInfo::getExtension (ScriptContext/*!*/context )
        //public SplFileInfo SplFileInfo::getFileInfo (ScriptContext/*!*/context [ string $class_name ] )
        //public string SplFileInfo::getFilename (ScriptContext/*!*/context )
        //public int SplFileInfo::getGroup (ScriptContext/*!*/context )
        //public int SplFileInfo::getInode (ScriptContext/*!*/context )
        //public string SplFileInfo::getLinkTarget (ScriptContext/*!*/context )
        //public int SplFileInfo::getMTime (ScriptContext/*!*/context )
        //public int SplFileInfo::getOwner (ScriptContext/*!*/context )
        //public string SplFileInfo::getPath (ScriptContext/*!*/context )
        //public SplFileInfo SplFileInfo::getPathInfo (ScriptContext/*!*/context [ string $class_name ] )
        //public string SplFileInfo::getPathname (ScriptContext/*!*/context )
        //public int SplFileInfo::getPerms (ScriptContext/*!*/context )
        //public string SplFileInfo::getRealPath (ScriptContext/*!*/context )
        //public int SplFileInfo::getSize (ScriptContext/*!*/context )
        //public string SplFileInfo::getType (ScriptContext/*!*/context )
        //public bool SplFileInfo::isDir (ScriptContext/*!*/context )
        //public bool SplFileInfo::isExecutable (ScriptContext/*!*/context )
        //public bool SplFileInfo::isFile (ScriptContext/*!*/context )
        //public bool SplFileInfo::isLink (ScriptContext/*!*/context )
        //public bool SplFileInfo::isReadable (ScriptContext/*!*/context )
        //public bool SplFileInfo::isWritable (ScriptContext/*!*/context )
        //public SplFileObject SplFileInfo::openFile (ScriptContext/*!*/context [ string $open_mode = r [, bool $use_include_path = false [, resource $context = NULL ]]] )
        //public void SplFileInfo::setFileClass (ScriptContext/*!*/context [ string $class_name ] )
        //public void SplFileInfo::setInfoClass (ScriptContext/*!*/context [ string $class_name ] )
        //public void SplFileInfo::__toString (ScriptContext/*!*/context )
    //}
}
