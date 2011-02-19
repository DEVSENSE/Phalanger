[expect php]

[file]
<?php
// The \\2 is an example of backreferencing. This tells pcre that
// it must match the second set of parentheses in the regular expression
// itself, which would be the ([\w]+) in this case. The extra backslash is 
// required because the string is in double quotes.
$html = "<b>bold text</b><a href=howdy.html>click me</a>";

preg_match_all("/(<([\w]+)[^>]*>)(.*)(<\/\\2>)/", $html, $matches);

for ($i=0; $i< count($matches[0]); $i++) {
  echo "matched: " . $matches[0][$i] . "\n";
  echo "part 1: " . $matches[1][$i] . "\n";
  echo "part 2: " . $matches[3][$i] . "\n";
  echo "part 3: " . $matches[4][$i] . "\n\n";
}
?> 