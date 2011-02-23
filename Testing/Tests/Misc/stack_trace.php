[expect]
array
(
  [0] => array
  (
    [line] => 6
    [column] => 3
    [file] => stack_trace.inc
    [function] => j
  )
  [1] => array
  (
    [line] => 4
    [column] => 3
    [file] => __input.txt inside eval (on line 11, column 1)
    [function] => i
  )
  [2] => array
  (
    [line] => 4
    [column] => 27
    [file] => __input.txt
    [function] => h
  )
  [3] => array
  (
    [line] => 4
    [column] => 27
    [file] => __input.txt
    [function] => array_map
  )
  [4] => array
  (
    [line] => 18
    [column] => 1
    [file] => __input.txt
    [function] => InlinedLambda
  )
  [5] => array
  (
    [line] => 18
    [column] => 1
    [file] => __input.txt
    [function] => f
  )
)
[file]
<?
function f()
{
  $g = create_function('','array_map("h",array(1));');

  $g();
}

include("stack_trace.inc");

eval('
function h()
{
  i();            // BUG: eval(i());
}
');

f();
?>