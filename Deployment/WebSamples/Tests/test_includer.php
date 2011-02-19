<?
//set_time_limit(10);
//putenv('TEST_PHP_EXECUTABLE=C:\Program Files\PHP\php.exe');

if (!isset($_GET['testdir'])) die('The testdir GET parameter has to be supported!');
$_path = $_GET['testdir'];

$_dirname = substr(strrchr($_path, '/'), 1);
$_dirname{0} = strtoupper($_dirname{0});

$_title = $_dirname;
$_source = isset($_GET['source']) && $_GET['source'] ? 1:0;
$_res = isset($_GET['result']) && $_GET['result'] ? 1:0;
$_xmp = isset($_GET['xmp']) && $_GET['xmp'] ? 1:0;
$_file = $_filename = isset($_GET['file']) ? $_GET['file']:'';
$_index = "<a href=\"index.php\">Tests Homepage</a>";

if ($_file)
{
  $_name = explode('.', basename($_file));
  $_function = $_name[0];
  $_title .= " ($_function)";
  $_file = $_path .'/'. $_file;
}

function _index_href($f, $s, $r, $x)
{
  global $_path;
  $script = $_SERVER['SCRIPT_NAME'];
  return "$script?testdir=$_path&file=$f&source=$s&result=$r&xmp=$x";
}

?>
<html>
<head>
<title>Test: <?=$_title?></title>
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
  <h1 class="dtH1"><?="$_dirname Functions"?></h1>
  </div>
</div>
<div id="nstext">

<p>
Use these links to toggle the display 
of <a href="<?echo _index_href($_filename, !$_source, $_res, $_xmp)?>">script source</a>
(<b><?echo $_source?'ON':'OFF'?></b>)

, <a href="<?echo _index_href($_filename, $_source, !$_res, $_xmp)?>">result</a>
(<b><?echo $_res?'ON':'OFF'?></b>)

and <a href="<?echo _index_href($_filename, $_source, $_res, !$_xmp)?>">result source</a>
(<b><?echo $_xmp?'ON':'OFF'?></b>).
</p>
<p>
Select another test group at the <?echo $_index?>.
</p>

<?
//echo "<pre>";var_dump($_GET);echo "</pre>";

function _index_showfile($filename, $text, $source, $res, $xmp, $output)
{
  if ($source || $res || $xmp)
    echo "  <h4 class=\"dtH4\">Script \"" . basename($filename) . "\"</h4>\n";
//    echo "  <h4 class=\"dtH4\">Script \"<a href=\"file://" . realpath($filename) . "\">" . basename($filename) . "</a>\"</h4>\n";

  echo $text;

  if ($source)
  {
    echo "  <h4 class=\"dtH4\">Script source</h4><p>\n";
    echo "  <pre class=\"code\">\n";
//    highlight_file($filename); // UNSUPPORTED
    echo htmlspecialchars(file_get_contents($filename));
    echo "  </pre></p>\n";
  }

  if ($res)
  {
    echo "  <h4 class=\"dtH4\">Result</h4><p>\n";
    echo $output;
    echo "  </p>\n";
  }

  if ($xmp)
  {
    echo "  <h4 class=\"dtH4\">Result source</h4><p>\n";
    echo "  <pre class=\"code\">\n";
    echo strtr($output, array(
      '<' => '&lt;',
      '>' => '&gt;',
      "\r\n" => "<b>\\r\\n</b>\r\n",
      "\n" => "<b>\\n</b>\n",
      "\r" => "<b>\\r</b>\r"
    ));
    echo "  </pre></p>\n";
  }
}

if ($_file)
{
  $man = "<a href=\"http://www.php.net/manual/en/function.$_function.php\">$_function</a>";
  $home = "<a href=\""._index_href('', 0, 0, 0)."\">$_dirname Functions</a>";
  
  $text = "";
  if ($_name[1] == 'php')
    $text = "  <p>The function $man is a part of $home.</p>\n";
  else if ($_name[1] == 'phpt')
    $text = "  <p>The file <b>$_file</b> is an original test script of $home.</p>\n";


  $output = "";
  if ($_res || $_xmp)
  {
    // include the file at global level (not in a function)

    ob_start();
var_dump($_file);
    include($_file);
    $output = ob_get_contents();
    ob_end_clean();
/**/
  }


  // one file with source and xmp
  _index_showfile($_file, $text, $_source, $_res, $_xmp, $output);
  echo "  <h4 class=\"dtH4\">See also</h4>\n";
  echo "  <p>$home, $_index</p>\n";
}
else
{
  // list all files in the directory
  $_dh = opendir($_path);
  $_first = 1;
  $_letter = '';
  $_display = $_source || $_res || $_xmp;
  while (($_scriptfile = readdir($_dh)) !== false) 
  {
    if ($_scriptfile{0} == '.') continue;
    if ($_scriptfile == 'index.php') continue;
    if (false === strstr($_scriptfile, '.php')) continue;
    
    // Show the index letter
    if (!$_display)
    {
      if ($_letter != strtoupper($_scriptfile{0}))
      {
        $_letter = strtoupper($_scriptfile{0});
        echo "  <h4 class=\"dtH4\">$_letter</h4>\n";
      }
    }
    else
    {
      if (!$_first) echo "  \n\n<hr>\n\n";
      $_first = 0;
	}
    
    $_scriptpath = $_path . '/' . $_scriptfile;
    $output = "";
    if ($_res || $_xmp)
    {
      // include the file at global level (not in a function)
      ob_start();
      include $_scriptpath;
      $output = ob_get_contents();
      ob_end_clean();
    }


    $text = "  <p>Open the <a href=\""._index_href($_scriptfile, 1, 1, 1)."\">$_scriptfile</a> tester.</p>\n";
    _index_showfile($_scriptpath, $text, $_source, $_res, $_xmp, $output);
  }
  closedir($_dh);

//  echo "  <h4 class=\"dtH4\">Done</h4>\n";
  echo "  <h4 class=\"dtH4\">See also</h4>\n";
  echo "  <p>$_index</p>\n";
}

?>

  <hr>
  <div id="footer"><p><a>The Phalanger Team</a></p>
    <p></p>
  </div>
</div>
</body>