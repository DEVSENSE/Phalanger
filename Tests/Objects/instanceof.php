[expect php]
[file]
<?

function __autoload($class)
{
  echo "AUTOLOAD $class\n";
}

$a = null;

echo is_subclass_of($a,"B") ? "yes" : "no","\n";
echo @($a instanceof A) ? "yes" : "no","\n";        // reports error


?>