<?php
	
	// calling library file system function example
	
	// write out size in MB
	$MB = 1048576;

	echo "Disk free space example.\n\n";
	
	foreach (range('C','F') as $drive)
	{
		echo "Drive $drive: ";
		$sp = disk_free_space("$drive:");
		
		if ($sp === false)
			echo "not present\n";
		else
			echo ((int)($sp/$MB))." MB free\n";
	}

	fgets(STDIN);
	
?>