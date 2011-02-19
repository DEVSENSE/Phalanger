[expect exact]
NULL
string(18) "exception_handler1"
string(18) "exception_handler2"
string(18) "exception_handler1"
Uncaught exception: hello world
[file]
<?
function exception_handler1($exception)
{
  echo "Uncaught exception: " , $exception->getMessage(), "\n";
}

function exception_handler2($exception)
{
  echo "Uncaught exception: " , $exception->getMessage(), "\n";
}

function exception_handler3($exception)
{
  echo "Uncaught exception: " , $exception->getMessage(), "\n";
}

var_dump(set_exception_handler('exception_handler1'));
var_dump(set_exception_handler('exception_handler2'));
var_dump(set_exception_handler('exception_handler3'));

restore_exception_handler();
restore_exception_handler();

var_dump(set_exception_handler('exception_handler3'));

class E extends Exception
{
  function __toString()
  {
    return "!!!";
  }
}

function f()
{
  throw new E("hello world");
}

f();

?>