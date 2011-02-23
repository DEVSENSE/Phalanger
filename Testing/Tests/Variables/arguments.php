[expect] 
Number of arguments: 6<br>
Second argument is: 2<br>
Argument 0 is: 1<br>
Argument 1 is: 2<br>
Argument 2 is: 3<br>
Argument 3 is: 4<br>
Argument 4 is: 5<br>
Argument 5 is: 6<br>
[file]
<?
  function A() 
  {
    $numargs = func_num_args(); 
    echo "Number of arguments: $numargs<br>\n"; 
    if ($numargs >= 2) 
    { 
      echo "Second argument is: " . func_get_arg (1) . "<br>\n"; 
    } 
    $arg_list = func_get_args(); 
    
    for ($i = 0; $i < $numargs; $i++) 
      echo "Argument $i is: " . $arg_list[$i] . "<br>\n"; 
  }

  A(1,2,3,4,5,6);
?>
