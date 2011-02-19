[expect php]

[file]
<?php

// v v over simplified
class foo
{
  function parse()
  {
   $pattern = "/<a(.*?)href\s*=\s*['|\"|\s*](.*?)['|\"|>](.*?)>(.*?)<\/a>/i";
   $string = "<a class='whatever' href='http://foo.com' target='_blank'>foo</a>";
   print preg_replace_callback($pattern,array($this,'cb'),$string);
  }

  function cb($matches)
  {
   return "<a" . $matches[1] . "href='http://someothersite.com/foo.php?page=" . $matches[2] . "'" . $matches[3] . ">" . $matches[4] . "</a>";
  }

}

$bar = new foo();
$bar->parse();

/**
output is
<a class='whatever' href='http://someothersite.com/foo.php?page=http%3A%2F%2Ffoo.com' target='_blank'>foo</a>
*/

?> 
