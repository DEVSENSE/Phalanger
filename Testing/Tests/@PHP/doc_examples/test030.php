[expect php]

[file]
<?php
$error_descriptions[E_ERROR]   = "A fatal error has occured";
$error_descriptions[E_WARNING] = "PHP issued a warning";
$error_descriptions[E_NOTICE]  = "This is just an informal notice";

foreach ($error_descriptions as $i)
{
	echo "$i ";
}
?>
