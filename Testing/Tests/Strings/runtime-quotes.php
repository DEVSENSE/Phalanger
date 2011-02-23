[expect php]
[file]
<?
function set($rt,$sb)
{
  ini_set("magic_quotes_runtime", $rt);
  ini_set("magic_quotes_sybase", $sb);
  echo 
    "magic_quotes_runtime = ",(int)ini_get("magic_quotes_runtime"),", ",
    "magic_quotes_sybase = ",(int)ini_get("magic_quotes_runtime"),"\n";
}

function read_test($file)
{
  $fp = fopen($file,"rt");

  $fread = fread($fp,10);
  echo "fread = ($fread)\n";

  @rewind($fp);
  
  $fgets = fgets($fp);
  echo "fgets = ($fgets)\n";

  fclose($fp);

  $array = file($file);
  echo "file[0] = (",$array[0],")\n";
}

function write_test($file)
{
  $text = "a\\'b\"\\\\c";

  $fp = fopen($file,"wt");

  $fwrite = fwrite($fp,$text);
  echo "fwrite = ($fwrite)\n";

  $fputs = fputs($fp,$text);
  echo "fputs = ($fputs)\n";

  fclose($fp);
}

function test_exec($cmd)
{
  echo `$cmd`;
  echo exec($cmd),"\n";
  echo shell_exec($cmd);
  passthru($cmd);
}

chdir(dirname(__FILE__));

set(1,1);
read_test("runtime-quotes.txt");

set(1,1);
write_test("runtime-quotes2.txt");

set(0,0);
read_test("runtime-quotes.txt");

set(0,0);
read_test("runtime-quotes2.txt");

set(1,1);
test_exec("echo e'e"); 

set(0,0);
test_exec("echo e'e"); 
?>