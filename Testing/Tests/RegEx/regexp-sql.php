[expect php]

[file]
<?
#public static string (string,bool)
function FormatSQL($SQL,$Highlight = true)
{  
  // indentation:
  $SQL = str_replace("\r","",$SQL);
  $lines = explode("\n",$SQL);

  $min = 10000;
  for ($i=1;$i<count($lines);$i++)
  { $dif = strlen($lines[$i]) - strlen(ltrim($lines[$i]));
    if ($dif<$min) $min = $dif;
  }
  
  if ($min>0)
    for ($i=1;$i<count($lines);$i++)
      $lines[$i] = substr($lines[$i],$min);
  
  $SQL = "\n".implode("\n",$lines); 

  if (!$Highlight) return $SQL;
  
  //Highlighting:
  $SqlKeywords = 
    // keywords:
    "SELECT|FROM|WHERE|UPDATE|SET|INSERT|INTO|CREATE|REPLACE|".
    "VALUES|DROP|CASCADE|DELETE|".
    "CALL|PROCEDURE|FUNCTION|TABLE|VIEW|PACKAGE|BODY|TRIGGER|SEQUENCE|".
    "AND|OR|NOT|IN|ON|AS|IS|TO|EXISTS|ORDER|GROUP|HAVING|BY|".
    "MOD|LIKE|NUMBER|VARCHAR|DATE|CHAR|CHARACTER|LONG|CONSTANT|BOOLEAN|".
    "BEGIN|END|DECLARE|IF|THEN|ELSE|LOOP|FOR|NULL|CHECK|".
    "PRIMARY|KEY|FOREIGN|REFERENCES|DETERMINISTIC|DEFFERABLE|".
    "REF|CURSOR|OPEN|INSTEAD|OF|EACH|ROW|INTO|RETURN|UNION|".
    "MINUS|ALL|SOME|ANY|GRANT|".
    "ALTER|MODIFY|PARTITION|TABLESPACE|INITIALLY|DEFERRED|COMMENT|".
    "FORCE|DEFAULT|IDENTIFIED|LANGUAGE|NAME|RETURNING|".
    "TRUE|FALSE|EXIT|WHEN|BEFORE|AFTER|INDEX|CLUSTER|".
    "EXCEPTION|OTHERS|AUTHID|CURRENT_USER|EXECUTE|IMMEDIATE|".
    "TYPE|WHILE|NEXT|FIRST|ELSIF|BETWEEN|FOUND|SQL|ROWNUM|".
    "NEW|OLD|CURRVAL|ACCESS|BFILE|TRANSACTION|BLOB|BULK|COLLECT|".
    "CLOB|CLOSE|RAW|ROWID|NCLOB|NCHAR|UROWID|RECORD|VARRAY|VARCHAR2|".
    "DEC|DECIMAL|DOUBLE|PRECISION|FLOAT|INTEGER|INT|NUMERIC|REAL|SMALLINT|".
    "PLS_INTEGER|USING|ASC|DESC|ROWNUM".
    // functions:    
    "ABS|ACOS|ADD_MONTHS|ATAN|ATAN2|CEIL|COS|COSH|EXP|FLOOR|".
    "LN|LOG|MOD|POWER|ROUND|SIGN|SIN|SINH|SQRT|TAN|TANH|".
    "CHR|CONCAT|INITCAP|LOWER|LPAD|LTRIM|NLS_INITCAP|".
    "NLS_LOWER|NLSSORT|NLS_UPPER|REPLACE|RPAD|RTRIM|SOUNDEX|".
    "SUBSTR|SUBSTRB|TRANSLATE|TRIM|UPPER|ASCII|INSTR|".
    "INSTRB|LENGTH|".
    "LENGTHB|ADD_MONTHS|LAST_DAY|MONTHS_BETWEEN|".
    "NEW_TIME|NEXT_DAY|".
    "SYSDATE|TRUNC|CHARTOROWID|CONVERT|HEXTORAW|RAWTOHEX|ROWIDTOCHAR|".
    "TO_CHAR|TO_DATE|".
    "TO_LOB|TO_MULTI_BYTE|TO_NUMBER|TO_SINGLE_BYTE|BFILENAME|DUMP|EMPTY_BLOB|".
    "EMPTY_CLOB|GREATEST|LEAST|".
    "NLS_CHARSET_DECL_LEN|NLS_CHARSET_ID|NLS_CHARSET_NAME|NVL|SYS_CONTEXT|".
    "SYS_GUID|UID|USER|USERENV|VSIZE|".
    "DEREF|MAKE_REF|".
    "REF|REFTOHEX|".
    "VALUE|AVG|COUNT|GROUPING|".
    "MAX|MIN|STDDEV|".
    "SUM|VARIANCE";
  
  $SQL = str_replace("/*","<I>/*",$SQL);
  $SQL = str_replace("*/","*/</I>",$SQL);
  $SQL = ereg_replace("([^A-Za-z0-9_])($SqlKeywords)([^A-Za-z0-9_])","\\1<B>\\2</B>\\3",$SQL);
  $SQL = ereg_replace("([^>A-Za-z0-9_])($SqlKeywords)([^<A-Za-z0-9_])","\\1<B>\\2</B>\\3",$SQL);
  $SQL = ereg_replace(":(new|old)",":<B>\\1</B>",$SQL);
  $SQL = ereg_replace("--([^\n]*)[\n]","<I>--\\1</I>\n",$SQL);
  $SQL = ereg_replace("'([^']*)'","<Q>'\\1'</Q>",$SQL);
  $SQL = ereg_replace("'([^']*)'","<Q>'\\1'</Q>",$SQL);
  $SQL = ereg_replace("%(TYPE|ROWCOUNT|ROWTYPE|NOTFOUND)","%<B>\\1</B>",$SQL);
  return $SQL;
} 

echo FormatSQL("SELECT * FROM MyTable WHERE x = 'hello'; /* comment */");
?>