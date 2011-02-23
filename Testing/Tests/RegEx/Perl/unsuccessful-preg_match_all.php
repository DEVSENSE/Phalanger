[expect php]
[file]
<?
	$code = "blah {BLAH} blah";
echo 
	preg_match_all('#\{(([a-z0-9\-_]+?\.)+?)([a-z0-9\-_]+?)\}#is', $code, $varrefs), "\n",
	count($varrefs), "\n";
	
$code = "blah {BLAH.kvak} blah";
echo 
	preg_match_all('#\{(([a-z0-9\-_]+?\.)+?)([a-z0-9\-_]+?)\}#is', $code, $varrefs), "\n",
	count($varrefs), "\n";
?>