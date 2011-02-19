<?php

$default_root = ".";
$path_to_tests = (isset($_GET['testroot'])) ? $_GET['testroot'] : $default_root;

?>


<html>
<head>
<title>Phalanger Tests Home</title>
<link rel="stylesheet" href="MSDN.css"/>
</head>
<body id="bodyID" class="dtBODY">

<div id="nsbanner">
  <div id="bannerrow1">
    <table class="bannerparthead" cellspacing="0">
      <tr id="hdr">
         <td class="runninghead">Phalanger Testing</td>
         <td class="product"><?echo $_SERVER['SERVER_NAME']?></td>
      </tr>
    </table>
  </div>
  <div id="TitleRow">
  <h1 class="dtH1">Phalanger Tests Home</h1>
  </div>
</div>
<div id="nstext">

  <p>Welcome to the <b>Phalanger</b> testing homepage.</p>
  <h4 class="dtH4">Available Test Sets</h4>
  <p>Please set the tests directory:</p>
  <p><form method="get">
  <input type="text" name="testroot" value="<?=$path_to_tests?>" />
  <input type="submit" value="Set" />
  </form></p>

  <h4 class="dtH4">Available Test Sets</h4>
  <p>

<?php

function scriptdir($path)
{
  $dh = opendir($path);
  while (($file = readdir($dh)) !== false)
  {	
	if (strpos($file, ".php") !== false) 
	{
		return true;
	}
  }
  return false;
}

function readtree($path, &$dirs)
{
  $dh = opendir($path);
  while (($dir = readdir($dh)) !== false)
  {	
	if ($dir{0} == '.') continue;
        if (is_file("$path/$dir/__skip")) return;
 	$newpath = "$path/$dir";
	if (!is_dir($newpath)) continue;
 	if (scriptdir($newpath)) $dirs[] = $newpath;
	readtree($newpath, $dirs);
  }
}

if (is_dir($path_to_tests))
{
  echo "The following test directories are available:</p>";
  echo "  <ul>";
  $dirs = array();
  $root_length = strlen($path_to_tests);
  if (($path_to_tests{$root_length-1} != '/') && ($path_to_tests{$root_length-1} != '\\'))
    $root_length++;
  readtree($path_to_tests, $dirs);

  foreach ($dirs as $dir)
  {	
	if (!is_dir($dir)) continue;
	if (($dir == '.') || ($dir == '..')) continue;
 	$Dir = substr($dir, $root_length);
	$Dir{0} = strtoupper($Dir{0});
	echo "    <li><a href=\"test_includer.php?testdir=$dir\">$Dir</a></li>\n";
  }
  echo "  </ul><p>";
}
else
{
  echo "Sorry, the path '<code>$path_to_tests</code>' is invalid.";
}
?>
  </p>
  <hr>
  <div id="footer"><p><a>The Phalanger Project Team</a></p>
    <p></p>
  </div>
</div>
</body>