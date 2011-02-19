[comment]
This code caused an overflow in converting ranges.

[expect php]

[file]
<?

$x = <<<EOT
/\(\(([-A-Za-z0-9 _+\/.,;:!?'"\[\]\{\}&À-ÿ]+)(\|[-A-Za-z0-9 _+\/.,;:!?'"\[\]\{\}&À-ÿ]+)?(\#[A-Za-z][-A-Za-z0-9_:.]*)?()\)\)/
EOT;
$y = "kfkaj +j k45øèíéìš3#$%^&kjakfjkaj|kfj";

  function process(&$matches)
  {
    return $matches[0];
  }
  
  echo "string(\"".preg_replace_callback($x,'process',$y)."\")";

?>
