[expect php]
[file]
<?
$context = stream_context_create();
echo "-------------------------\n";
echo file_get_contents(__FILE__),"\n";
echo "-------------------------\n";
echo file_get_contents(__FILE__,false,$context,1),"\n";
echo "-------------------------\n";
echo file_get_contents(__FILE__,false,$context,1,10),"\n";
?>
