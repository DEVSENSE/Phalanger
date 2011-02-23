<?

function __var_dump($x)
{
  __var_dump_rec("", $x);
}

function __var_dump_rec($indent, $x)
{
  if (is_array($x) || is_object($x))
  {
    echo $indent,"array\n";
    echo "$indent{\n";
    
    foreach ($x as $k => $v)
    {
      echo "$indent  ";
      if (is_string($k)) echo "'$k'"; else echo "$k";
      echo " => ";
      
      if (is_array($v) || is_object($v))
      {
        echo "\n";
        __var_dump_rec("$indent  ", $v);
      }  
      else
      {
        __var_dump_rec("", $v);
      }  
    }
    
    echo "$indent}\n";
  }
  else if (is_string($x))
    echo "{$indent}'$x'\n";
  else if (is_null($x))
    echo "{$indent}NULL\n"; 
  else if (is_bool($x))
    echo $x ? "{$indent}TRUE\n" : "{$indent}FALSE\n"; 
  else if (is_resource($x))
    echo "{$indent}resource '".get_resource_type($x)."'\n";
  else if (is_double($x))
    printf("{$indent}%.10f\n",$x); else
    echo "{$indent}$x\n";
}

?>