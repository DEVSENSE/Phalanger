[expect php]
[file]
<?
var_dump(basename('C:\x\y\z.php','.php'));
var_dump(basename('C:\x\y\z.php/','.php'));
var_dump(basename('C:\x\y/','.php'));
var_dump(basename('C:\x\y/'));
var_dump(basename('/////'));
var_dump(basename('m/////','m'));
var_dump(basename('m/////','mm'));
var_dump(basename('a/b/c/d/e/////'));
var_dump(basename('/xab////','xab'));
var_dump(basename('/xab////','ab'));
var_dump(basename('/**////','*'));
?>