[expect exact]
string(1) "C"
double(1.54)
double(Infinity)
654564.145400
Sat Saturday Apr April
--------------
string(19) "Danish_Denmark.1252"
double(1.54)
double(Infinity)
654564,145400
lo lordag apr april
--------------
string(25) "Czech_Czech Republic.1250"
double(1.54)
double(Infinity)
654564,145400
so sobota IV duben
--------------
[file]
<?
  date_default_timezone_set("Europe/Prague");
  foreach (array("","da-DK","cs-CZ") as $v)
  {
    var_dump(setlocale(LC_ALL,$v));
    var_dump(1.54,INF);
    printf("%f\n",654564.1454);
    echo strftime("%a %A %b %B",482194654),"\n";
    echo "--------------\n";
  } 
?>  
