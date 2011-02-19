[expect php]
[file]
<?php
function folder($folder)
{
 unset($rv);
 $handle = opendir($folder);
 while (false !== ($file = readdir($handle)))
 {
  $rv[] = $file;
 }
 closedir($handle);
 sort($rv);
 return $rv;
}
 
function printme($a)
{
foreach ($a as $k => $v) echo "[$k] => $v\n";
}

printme(folder("C:\\"));
printme(folder("."));
?>