[expect php]
[file]
<?
ob_start();

echo "hello";

$x = ob_get_contents();

ob_end_clean();

echo "Content: $x";
?>