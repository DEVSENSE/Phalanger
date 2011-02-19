[expect php]
[file]
<?
function d($lineno,$line)
{
  foreach ($line as $idx => $field)
  {
    echo $lineno,".",$idx,": '",addcslashes($field,"\n\t\r"),"'\n";
  }
}

function read_file($filename)
{
  echo "--- $filename ---\n";

  $fp = fopen($filename,"rt");
  
  if (!$fp) die("File not found!");

  $lineno = 0;
  while (($line = fgetcsv($fp,1000,";",'"')) !== false)
    d($lineno++,$line);

  fclose($fp);
}

chdir(dirname(__FILE__));

//read_file("cvs.test1.csv");
//read_file("cvs.test2.csv");

echo "\n--- write ---\n";

$fp = fopen("out.csv","wt");

if (!$fp) die("File not found!");

echo 
  fputcsv($fp,array("hello\nworld",'aaa"bbb"ccc','blah blah;','none',"tab\ttab",'space space'),';','"'),"\n",
  fputcsv($fp,array("hh'ee","ss\\nsadasdasd s"),';','"'),"\n",
  fputcsv($fp,array('""""""""""','"','','""','xxx"'),';','"'),"\n",
  fputcsv($fp,array('x')),"\n",
  fputcsv($fp,array('x\y')),"\n";

fclose($fp);

echo "\n--- read ---\n";

$fp = fopen("out.csv", "rt");

if (!$fp) die("File not found!");

while (($line = fgets($fp))!==false)
  echo addcslashes($line,"\n\t\r"),"\n";

fclose($fp);

unlink("out.csv");
?>