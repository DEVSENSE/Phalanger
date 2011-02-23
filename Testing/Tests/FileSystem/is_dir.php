[expect php]
[file]

<?
var_dump(is_dir('a_file.txt')) . "\n";
var_dump(is_dir('bogus_dir/abc')) . "\n";

var_dump(is_dir('..')); //one dir up
?> 