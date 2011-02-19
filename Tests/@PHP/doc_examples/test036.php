[expect php]

[file]
<?php
// fill an array with all items from a directory
$handle = opendir('c:\\');
while (false !== ($file = readdir($handle))) {
    $files[] = $file;
}
closedir($handle); 
?>

<?php
sort($files);
foreach ($files as $key => $value)
{
	echo "[$key]: $value\n";
}
?>
