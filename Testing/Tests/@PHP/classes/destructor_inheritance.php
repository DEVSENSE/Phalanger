[expect php]
[file]
<?

class base {
   function __construct() {
      echo __METHOD__ . "\n";
   }
   
   function __destruct() {
      // Phalanger: not deterministic echo __METHOD__ . "\n";
   }
}

class derived extends base {
}

$obj = new derived;

unset($obj);

echo 'Done';
?>