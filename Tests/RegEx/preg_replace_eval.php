[expect php]
[file]
<?
echo preg_replace("/([a-z\"]+)([0-9]+)/e", '"$1"."$2"', "ab\"01300 as\"00da sd\"0asdsa das asd 11asd a1sd a"),"\n";
echo preg_replace("/([a-z])([a-z])([a-z])([a-z])([a-z])([a-z])([a-z])([a-z])([a-z])([a-z])([a-z])([a-z])/e", 
  '\'(${0},$$1,\\,\l,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,${120})\'', "ab\"01300 as\"00da sd\"0aasdasdkjaskldjaklsdjalkdjdsa das asd 11asd a1sd a");
?>
