[expect php]
[file]
<?
print_r(array(true,false,1,1.2,null,"asdas",array(1),new stdClass,STDIN));
var_dump(array(true,false,1,1.2,null,"asdas",array(1),new stdClass,STDIN));
var_export(array(true,false,1,1.2,null,"asdas",array(1),new stdClass,STDIN));
?>