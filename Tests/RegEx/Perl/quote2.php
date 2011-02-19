[expect php]

[file]
<?php
// In this example, preg_quote($word) is used to keep the
// asterisks from having special meaning to the regular
// expression.

$textbody = "This book is *very* difficult to find.";
$word = "*very*";
$textbody = preg_replace ("/" . preg_quote($word) . "/",
                         "<i>" . $word . "</i>",
                         $textbody);
echo ($textbody);
?> 