[expect php]
[file]
<?

/*

  Tests meridian spec.

*/
putenv("TZ=Europe/Prague");
foreach (array(null,"am","pm") as $m)
{
  for($h=0;$h<=24;$h++)
  {
    @$ts = strtotime("$h:00:00$m 1/1/2005");
    echo "$h$m: ",($ts!==false) ? date("H:i:s m/d/Y",$ts) : "error","\n";
  }  
} 
?>