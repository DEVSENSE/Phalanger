[expect php]
[file]
<?  
	foreach (glob('../*', GLOB_ONLYDIR) as $folder)
	{
		var_dump($folder);
	}
?>