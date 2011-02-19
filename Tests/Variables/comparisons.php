[expect php]
[file]
<?
  $op = array(
    0.0,1.2,-8.7,
    0,1,-5,
    false,true,
    "","0","0.0","0.1","x","y",".",
    null,
    array(),array(0),array(1),array(1,2)
  );
  
  $str = array(
    '0.0','1.2','-8.7',
    '0','1','-5',
    'false','true',
    '""','"0"','"0.0"','"0.1"','"x"','"y"','"."',
    'null',
    'array()','array(0)','array(1)','array(1,2)'
  );
  
  for($i=0;$i<count($op);$i++)
  {
    for($j=0;$j<count($op);$j++)
    {
      echo $str[$i];

      $x = $op[$i];
      $y = $op[$j];

      echo ($x < $y) ? " < ":"";
      echo ($x == $y) ? " = ":"";
      echo ($x > $y) ? " > ":"";
    
      echo $str[$j];

      echo "\n";  
    }
  } 
?>  