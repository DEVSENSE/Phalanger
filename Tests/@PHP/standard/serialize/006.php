[expect php]
[file]
<?php
include('Phalanger.inc');
	$š = array('ì' => 'ì');

	class ì 
	{
		public $ý = 'ø';
	}
  
    $foo = new ì();
  
	__var_dump(serialize($foo));
	__var_dump(unserialize(serialize($foo)));
	__var_dump(serialize($š));
	__var_dump(unserialize(serialize($š)));
?>
