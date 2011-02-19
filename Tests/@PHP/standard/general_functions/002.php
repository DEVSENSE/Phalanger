[expect php]
[file]
<?php 
  echo bin2hex(quoted_printable_decode("=FAwow-factor=C1=d0=D5=DD=C5=CE=CE=D9=C5=0A=\r\n=20=D4=cf=D2=C7=CF=D7=D9=C5=\r\n=20=\r\n=D0=\r\n=D2=CF=C5=CB=D4=D9")); 
?>