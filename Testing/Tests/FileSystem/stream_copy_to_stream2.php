[expect php]
[file]

<?php

function localhost($r, $w)
{
 echo "<p>Read <b>$r</b>, Write <b>$w</b>\n";
 $fr = @fopen("http://www.google.com/", $r);
 if ($fr === false) die('NO NETWORK CONNECTION!');

 $fw = fopen("stream_copy_to_stream_${r}_${w}.txt", $w);
 echo "\n\nCOPIED: <b>" . stream_copy_to_stream($fr, $fw) . "</b>\n";
 fclose($fr);
 fclose($fw);

 /*

 $f = fopen("stream_copy_to_stream_${r}_${w}.txt", "rb");
 while (false !== ($c = fgetc($f)))
 {
 $c = (string)$c;
 //echo ord($c);
   if ($c == "\n") echo "[\\n]\n";
   else if ($c == "\r") echo "[\\r]\r";
   else if ($c == "<") echo "&lt;";
   else if ($c == ">") echo "&gt;";
   else echo $c;
 }
 fclose($f);

 */
 unlink("stream_copy_to_stream_${r}_${w}.txt");
}

//localhost("rt", "wt"); // Note: PHP ignores the read text mode
//localhost("rt", "wb"); // Note: PHP ignores the read text mode
localhost("rb", "wb");
localhost("rb", "wt");

//fgets(STDIN);
?> 