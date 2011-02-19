[expect php]
[file]
<?
error_reporting(0);
$var="This is a string";

$dummy="";
unset($dummy);

foreach($var['nosuchkey'] as $v) {
}
?>