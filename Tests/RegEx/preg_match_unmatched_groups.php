[expect php]
[file]
<?
function d($x)
{
  if (is_array($x))
  {
    echo "array {\n";
    foreach($x as $k => $v) 
    {
      echo "  $k => "; 
      d($v);
    }  
    echo "};";
  }
  elseif (is_null($x))
  {
    echo "NULL";
  }
  else
  {
    echo "'$x'";
  }
  echo "\n";
}

d(preg_match("/([a-z]+) ([a-z]*)[0-9]* ([a-z]+) ([a-z]+)?/","aaa 555 bbb 555",$m));
d($m);
d(preg_match("/([a-z]*)( ([a-z]*)( ([a-z]*))?)?/","aaa bbb",$m));
d($m);
d(preg_match("/([a-z]*)( ([a-z]*)( ([a-z]*))?)?/","aaa bbb",$m,PREG_OFFSET_CAPTURE));
d($m);
d(preg_match("/([a-z]*)( ([a-z]*)( ([a-z]*))?)?/","564564654",$m));
d($m);
d(preg_match("/([a-z]*)( ([a-z]*)( ([a-z]*))?)?/","654564654",$m,PREG_OFFSET_CAPTURE));
d($m);

?>