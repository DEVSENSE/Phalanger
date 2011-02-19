[expect php]
[file]
<?php
	require_once('tar.class.inc');

	@main();
	
	function main()
	{
		$tar = new tar();
		$tar->addFile('./test.pdf');
		$tar->toTar('./test_create.tar', false);

		$tar1 = new tar();
		$opened = $tar1->openTAR('./test_create.tar');
		if ($opened)
		{
			$soubor = $tar1->getFile('./test.pdf');
			echo $soubor["file"];
		}
		else
		{
			echo "FAILED TO OPEN ARCHIVE";
		}
	}
?>