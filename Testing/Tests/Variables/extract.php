[expect php]
[file]
<?  
	require('Phalanger.inc');
  function extract_test($array,$type,$prefix)
  {
    ${"a d"} = "hello";
    ${0} = "hello";
    ${""} = "hello";
    $ad = null;
    ${"000g"} = "hello";
    $prefix_ad = "hello";
      
    __var_dump(extract($array,$type,$prefix));
    unset($array,$type,$prefix);
    $vars = get_defined_vars();
    ksort($vars,SORT_STRING);
    __var_dump($vars);
  }

  $a = array(12,"a d" => 1,"ad" => 1,"0" => 1,"ad" => 2,"non_existent" => 1, "" => 1,"000g" => 1);

  echo "EXTR_PREFIX_ALL\n";
  extract_test($a,EXTR_PREFIX_ALL,"prefix");
  echo "<hr>";
  
  echo "EXTR_PREFIX_INVALID\n";
  extract_test($a,EXTR_PREFIX_INVALID,"prefix");
  echo "<hr>";
  
  echo "EXTR_PREFIX_SAME\n";
  extract_test($a,EXTR_PREFIX_SAME,"prefix");
  echo "<hr>";
  
  echo "EXTR_PREFIX_IF_EXISTS\n";
  extract_test($a,EXTR_PREFIX_IF_EXISTS,"prefix");
  echo "<hr>";
?>