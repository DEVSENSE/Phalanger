[expect php]

[additional scripts]
6C.inc

[file]
<?
  echo "6A.php\n";
  include_once("6B.inc");
  $x = "6C.inc";
  include($x);
?>