[expect php]
[file]
<?php
include('Phalanger.inc');
$test = array("A\x00B" => "Hello world");
__var_dump($test);
?>