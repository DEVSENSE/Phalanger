[expect php]

[file]
<?

var_dump( (int)(sin("1.0") * 100) == 84 );
var_dump( (int)(log(10)*100) == 230 );
var_dump( substr("1.0", 1, 1) );

?>