[expect php]
[file]
<?
$x = '/* tooltips and access keys */
ta = new Object();
ta[\'pt-userpage\'] = new Array(\'.\',\'My user page\');
ta[\'pt-anonuserpage\'] = new Array(\'.\',\'The user page for the ip you\\\'re editing as\');';

ini_set("magic_quotes_sybase",0);
echo addslashes($x), "\n\n";
ini_set("magic_quotes_sybase",1);
echo addslashes($x), "\n\n";

?>