[expect php]
[file]
<?
echo implode(array())."\n";
echo implode('nothing', array())."\n";
echo implode(array('foo', 'bar', 'baz'))."\n";
echo implode(':', array('foo', 'bar', 'baz'))."\n";
?>