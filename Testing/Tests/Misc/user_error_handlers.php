[expect php]
[file]
<?
  function h($errno, $errstr, $errfile, $errline)
  {
    echo "ERROR h\n";
  }
  
  function g($errno, $errstr, $errfile, $errline)
  {
    echo "ERROR g\n";
  }

  function j($errno, $errstr, $errfile, $errline)
  {
    echo "ERROR j\n";
  }
  
  restore_error_handler();
	set_error_handler("h");
  set_error_handler("g");
  restore_error_handler();
	set_error_handler("h");
  set_error_handler("j");
  restore_error_handler();
	set_error_handler("h");
  restore_error_handler();
	set_error_handler("j");
  restore_error_handler();
	echo set_error_handler("j"),"\n";
	array_fill(-1,-1,-1);
?>