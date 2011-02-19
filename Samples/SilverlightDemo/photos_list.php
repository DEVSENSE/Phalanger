<?
	// Photo listing 
	//   ?loc=dir  - list files in the directory
	//   otherwise - list directories in the 'photos' dir
	$loc = $_GET["loc"];
	$root = $loc == "";
	if (!$root) $dir = "photos/".$loc; else $dir="photos";
	
	function getFirstFile($dir)
	{
		$dh = dir($dir);
		while(($file = $dh->read()) !== false) 
		{
			if (substr($file, 0, 1) != '.')
			{
				$dh->close();
				return $file;
			}
		}
	}
	
	// Write all files/directories
	$dh = dir($dir);
  while(($file = $dh->read()) !== false) 
  {
		if (substr($file, 0, 1) != '.')
		{
			echo "$file\n";
			if ($root) echo getFirstFile("photos/$file")."\n";
		}
  }
  $dh->close();	
?>