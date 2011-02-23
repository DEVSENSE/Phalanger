[expect php]
[file]
<?php

$data = "Two Ts and one F.";

$result = count_chars($data, 0);

for ($i=0; $i < count($result); $i++) {
   if ($result[$i] != 0)
       echo "There were $result[$i] instance(s) of \"" , chr($i) , "\" in the string.\n";
}

?> 