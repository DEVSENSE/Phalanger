[expect php]

[file]
<?

preg_match("/a*/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a*?/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a+/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a+?/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a?/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a??/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a{2,}/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a{2,}?/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a{2,3}/U", "aaa", $res);
echo $res[0]."\n";

preg_match("/a{2,3}?/U", "aaa", $res);
echo $res[0]."\n";


?>