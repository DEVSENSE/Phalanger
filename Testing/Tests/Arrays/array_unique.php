[expect php]
[file]
<?
  
  function test(&$a)
  {
    $arr = array( 4,3,4,3,2,1,2,1,4,3,2,1 );
    $arr[] = &$a;
    $arr[] = $a;
    var_dump($arr);

    // check whether values are preserved in the same order
    // without duplicities
    // preserving the first unique entry
    var_dump(array_unique($arr));
  }

  $x = 5;
  test($x);

?>