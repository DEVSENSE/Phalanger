<?php

// Changes directory to that of THIS include file.
chdir(dirname(__FILE__));

function printr($a, $indent = "")
{
  $nextindent = "$indent  ";
  $rv = "[ARRAY\n";
  foreach ($a as $k => $v) 
    $rv .= $nextindent . dump($k) . " => " . dump($v, $nextindent) . "\n";
  return "$rv$indent]";
}

function dump($a, $indent = "")
{
  if (!isset($a)) return "[UNDEFINED]";
  else if (is_bool($a)) return ($a) ? "[TRUE]":"[FALSE]";
  else if (is_float($a)) return sprintf("[DOUBLE (%01.6f)]", $a);
  else if (is_int($a)) return "[INT ($a)]";
  else if (is_string($a)) return "[STRING: \"$a\"]";
  else if (is_array($a)) return printr($a, $indent);
  else return "[??? ($a)]";
}

function d($a)
{
  echo "<pre>" . dump($a) . "</pre>\n";
}

?> 
