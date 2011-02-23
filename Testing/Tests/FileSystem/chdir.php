[expect php]
[file]
<?php

function dump($bool)
{
 echo ($bool) ? "TRUE" : "FALSE";
}

function _chdir($p)
{
  echo "<p><code>chdir($p)</code><br />\n";
  dump(chdir($p));
  echo getcwd() . "<br />\n";
}

$dir = getcwd();
_chdir('..');
_chdir('/');
_chdir($dir);

?>