[expect php]
[file]
<?
function ds($s)
{
  return ($s === null) ? "NULL" : $s;
}

function d($x)
{
  echo @ds($x['dirname']),";",@ds($x['basename']),";",@ds($x['extension']),"\n";
}

d(pathinfo(null));
d(pathinfo(""));
d(pathinfo('C:\x\y\z.php.info'));
d(pathinfo('C:\x\y\z.php.info..'));
d(pathinfo('./.'));
d(pathinfo('C:./.'));
d(pathinfo('./'));
d(pathinfo('C:\x\y\z.php/'));
d(pathinfo('C:\x\y/'));
d(pathinfo('C:\x\y/'));
d(pathinfo('/////'));
d(pathinfo('m/////'));
d(pathinfo('m/////'));
d(pathinfo('a/b/c/d/e/////'));
d(pathinfo('/xab////'));
d(pathinfo('/xab////'));
d(pathinfo('/**////'));
d(pathinfo("c"));
d(pathinfo("c:"));
d(pathinfo("c:x"));
d(pathinfo("c:\\"));
d(pathinfo("c:\\///"));
d(pathinfo("c:/"));
d(pathinfo("c:\\x"));
d(pathinfo("c:\\x\\"));
d(pathinfo("c:\\x\\y.l"));
d(pathinfo("c:\\x\\y\\"));
d(pathinfo("c:\\x/y\\"));
d(pathinfo("c:\\x\\y\\..\\"));
d(pathinfo("c:\\x\\y\\.\\/\\..\\u"));
d(pathinfo("c:\\x\\y\\.\\/\\..\\u\\\\\\"));
d(pathinfo("c:\\x\\%path%/y\\.\\*.*/\\..\\u"));
d(pathinfo("\\")); 
d(pathinfo("/")); 
d(pathinfo("a/")); 
d(pathinfo("a/b")); 
d(pathinfo("a/b/")); 
d(pathinfo("a/b/c")); 
d(pathinfo("a/b/c\\d.")); 
d(pathinfo("a/b\\c/d")); 
d(pathinfo("a\\b\\c/d")); 
?>