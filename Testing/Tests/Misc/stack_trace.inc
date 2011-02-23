<?

function i()
{
  $x = "j";
  $x();
}

function j()
{
  $a = debug_backtrace();
  $b = array();
  foreach ($a as $frame)
  {
    $frame["file"] = substr($frame["file"],strrpos($frame["file"],"\\")+1);
    $b[] = $frame;
  }
  print_r($b);
}

?>