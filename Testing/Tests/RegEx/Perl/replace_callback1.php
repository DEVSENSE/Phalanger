[expect php]

[file]
<?php
  // this text was used in 2002
  // we want to get this up to date for 2003
  $text = "April fools day is 04/01/2002\n";
  $text.= "Last christmas was 12/24/2001\n";

  // the callback function
  function next_year($matches) 
  {
   // as usual: $matches[0] is the complete match
   // $matches[1] the match for the first subpattern
   // enclosed in '(...)' and so on
   return $matches[1].($matches[2]+1);
  }

  echo preg_replace_callback(
             "|(\d{2}/\d{2}/)(\d{4})|",
             "next_year",
             $text);

  // result is:
  // April fools day is 04/01/2003
  // Last christmas was 12/24/2002
?> 