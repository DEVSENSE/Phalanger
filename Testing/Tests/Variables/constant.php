[expect php]
[file]
<?
define("a",3);
echo a." ".defined("a")." ".constant("a");
?>