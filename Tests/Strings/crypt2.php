[expect php]
[file]
<?
echo 
  crc32("hello"),"\n";

echo 
  md5("hello"),"\n",
  bin2hex(md5("hello",true)),"\n",
  md5_file(__FILE__),"\n";

echo 
  sha1("hello"),"\n",
  bin2hex(sha1("hello",true)),"\n",
  sha1_file(__FILE__),"\n";
?>