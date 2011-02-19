[expect php]

[file]
<?
function valid_ipv4($ip_addr)
{
       $num="([0-9]|^1?\d\d$|2[0-4]\d|25[0-5])";

       if(preg_match("/$num\.$num\.$num\.$num/",$ip_addr,$matches))
       {
               return $matches[0];
       } else {
               return false;
       }
}

var_dump(valid_ipv4("241.25.16.22"));
var_dump(valid_ipv4("256.41.15.11"));

?> 

