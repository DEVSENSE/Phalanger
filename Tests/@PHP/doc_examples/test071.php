[expect php]

[file]
<?php
$output = `echo ahoj`;
echo "<pre>$output</pre>";

$output = `type non_existing_file`;
echo "<pre>$output</pre>";
?>
