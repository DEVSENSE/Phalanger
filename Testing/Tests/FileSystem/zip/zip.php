[expect php]
[file]
<?php

$zip = zip_open('./test.zip');

if (is_resource($zip)) {
  // consider zip file opened successfully
  echo "correct";
  zip_close($zip);
}
else
{
	echo "failed";
}

?>

