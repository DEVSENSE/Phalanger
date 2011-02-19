[comment] We have different mtime and ctime sometimes... Not tested for output, just for compiling and running.

[file]

<pre>
<?php
function folder($folder)
{
 unset($rv);
 $handle = opendir($folder);
 while (false !== ($file = readdir($handle)))
 {
  $rv[] = $folder.$file;
 }
 closedir($handle);
 sort($rv);
 return $rv;
}

$statfields = array(
"dev",
"ino",
//"mode",	// Directory w sometimes not set - why?
"uid",
"gid",
"rdev",
"size",
//"atime", 	// Minor difference - why?
"mtime",
"ctime",
"blksize",
"blocks"
);

function getstat($f)
{
  global $statfields; 

  $a = stat($f);
  $rv = "!!$f";

  foreach ($statfields as $key)
    $rv .= "\n    $key => " . $a[$key];

//  $rv .= "\n    MODE => " . decbin($a['mode']);

  return $rv;
} 
 
function printdir($a, $n)
{
echo "<hr><p>" . realpath($n) . "\n";
foreach ($a as $k => $v) echo "  [$k] => stat($v)\n  (" .getstat($v). "\n  )\n";
}

printdir(folder("C:\\"), "C:\\");
//printdir(folder("./"), "./");
?>
</pre>