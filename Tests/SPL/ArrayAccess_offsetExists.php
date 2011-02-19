[expect php]
[file]
<?
class ArrayClass implements ArrayAccess 
{
    private $index;
    public $x;
    
    function __construct($index)
    {
      $this->index = $index;
      $this->x =& $GLOBALS["x"];
    } 

    function offsetGet($index) 
    {
        echo "{$this->index}: offsetGet($index)\n";
        return new ArrayClass($this->index + 1);
    }
    
    function offsetSet($index, $newval) 
    { 
      echo "{$this->index}: offsetSet($index,$newval)\n";
    }
    
    function offsetExists($index) 
    { 
      echo "{$this->index}: offsetExists($index)\n";
      return "";
    }
    
    function offsetUnset($index)
    {
      echo "{$this->index}: offsetUnset($index)\n";
    }
}

$x = new ArrayClass(100);
$obj = new ArrayClass(0);

var_dump(isset($obj));
var_dump(isset($obj[10][2]->x[1]->x[8][10][1]));
var_dump(isset($obj[1][2][3]));
var_dump(isset($obj[1]));

var_dump(empty($obj));
var_dump(empty($obj[10][2]->x[1]->x[8][10][1]));
var_dump(empty($obj[1][2][3]));
var_dump(empty($obj[1]));
?>