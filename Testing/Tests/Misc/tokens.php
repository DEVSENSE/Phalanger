[expect php]
[file]
<?
$consts = get_defined_constants();
$tokens = array();
foreach ($consts as $name => $value)
  if ($name[0] == "T" && $name[1] == "_")
    $tokens[$name] = $value;
    
asort($tokens);    
foreach ($tokens as $name => $value)
  echo "$name = $value,\n";
?>