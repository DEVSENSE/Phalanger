[comment]
[expect php]
[file]


<?php
echo addcslashes('foo[ ]', 'A..z');
// output:  \f\o\o\[ \]
?>

Issues a Warning:
// echo addcslashes("zoo['.']", 'z..A');
// output:  \zoo['\.']
