[expect php]
[file]
<?

function display_tokens($tokens)
{
  foreach ($tokens as $token)
  { 
    if (is_array($token))
    {
      echo token_name($token[0]),"\n";
      echo $token[0]," '",htmlentities(addcslashes($token[1],"\n\r\t")),"'\n";
    }
    else
    {
      echo "    '",htmlentities($token),"'\n";
    }  
  }
}

$code = '
<? 

final class A 
{ 
  function __get($x) 
  { 
    $x = <<<EOOOOOOOOOOOOOOT
         <<<hello
EOOOOOOOOOOOOOOt;
EOOOOOOOOOOOOOT;
EOOOOOOOOOOOOOOT;

    $$y = array (1,100000000000000,999999999999999999999999999999999,true,false);
    ${"x${${"xxx"}}x"} = 1;
    ${"x${${"x$x->$$$x0xx"}}x"} = 1;
  } 
}

?>';

display_tokens(token_get_all($code));

?>