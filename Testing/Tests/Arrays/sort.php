[expect]

sort regular:
array
(
  [0] => 0x10
  [1] => 10x
  [2] => 20x
  [3] => ach0
  [4] => add1
  [5] => x10
  [6] => 0
  [7] => x2
  [8] => 1
  [9] => 1
  [10] => 8
)

sort numeric:
array
(
  [0] => x10
  [1] => 0
  [2] => x2
  [3] => ach0
  [4] => add1
  [5] => 1
  [6] => 1
  [7] => 8
  [8] => 10x
  [9] => 0x10
  [10] => 20x
)

sort string:
array
(
  [0] => 0
  [1] => 0x10
  [2] => 1
  [3] => 1
  [4] => 10x
  [5] => 20x
  [6] => 8
  [7] => ach0
  [8] => add1
  [9] => x10
  [10] => x2
)

sort locale:
array
(
  [0] => 0
  [1] => 0x10
  [2] => 1
  [3] => 1
  [4] => 10x
  [5] => 20x
  [6] => 8
  [7] => add1
  [8] => ach0
  [9] => x10
  [10] => x2
)

ksort regular:
array
(
  [x] => 8
  [z] => 1
  [0] => x10
  [1] => 10x
  [2] => 20x
  [3] => x2
  [4] => 0x10
  [5] => ach0
  [6] => add1
  [10a] => 0
  [2b] => 1
)

ksort numeric:
array
(
  [x] => 8
  [z] => 1
  [0] => x10
  [1] => 10x
  [2b] => 1
  [2] => 20x
  [3] => x2
  [4] => 0x10
  [5] => ach0
  [6] => add1
  [10a] => 0
)

ksort string:
array
(
  [0] => x10
  [1] => 10x
  [10a] => 0
  [2] => 20x
  [2b] => 1
  [3] => x2
  [4] => 0x10
  [5] => ach0
  [6] => add1
  [x] => 8
  [z] => 1
)

ksort locale:
array
(
  [0] => x10
  [1] => 10x
  [10a] => 0
  [2] => 20x
  [2b] => 1
  [3] => x2
  [4] => 0x10
  [5] => ach0
  [6] => add1
  [x] => 8
  [z] => 1
)

asort regular:
array
(
  [4] => 0x10
  [1] => 10x
  [2] => 20x
  [5] => ach0
  [6] => add1
  [0] => x10
  [10a] => 0
  [3] => x2
  [z] => 1
  [2b] => 1
  [x] => 8
)

asort numeric:
array
(
  [0] => x10
  [10a] => 0
  [3] => x2
  [5] => ach0
  [6] => add1
  [z] => 1
  [2b] => 1
  [x] => 8
  [1] => 10x
  [4] => 0x10
  [2] => 20x
)

asort string:
array
(
  [10a] => 0
  [4] => 0x10
  [z] => 1
  [2b] => 1
  [1] => 10x
  [2] => 20x
  [x] => 8
  [5] => ach0
  [6] => add1
  [0] => x10
  [3] => x2
)

asort locale:
array
(
  [10a] => 0
  [4] => 0x10
  [z] => 1
  [2b] => 1
  [1] => 10x
  [2] => 20x
  [x] => 8
  [6] => add1
  [5] => ach0
  [0] => x10
  [3] => x2
)

rsort regular:
array
(
  [0] => 8
  [1] => 1
  [2] => 1
  [3] => x2
  [4] => x10
  [5] => 0
  [6] => add1
  [7] => ach0
  [8] => 20x
  [9] => 10x
  [10] => 0x10
)

rsort numeric:
array
(
  [0] => 20x
  [1] => 0x10
  [2] => 10x
  [3] => 8
  [4] => 1
  [5] => 1
  [6] => x10
  [7] => 0
  [8] => x2
  [9] => ach0
  [10] => add1
)

rsort string:
array
(
  [0] => x2
  [1] => x10
  [2] => add1
  [3] => ach0
  [4] => 8
  [5] => 20x
  [6] => 10x
  [7] => 1
  [8] => 1
  [9] => 0x10
  [10] => 0
)

rsort locale:
array
(
  [0] => x2
  [1] => x10
  [2] => ach0
  [3] => add1
  [4] => 8
  [5] => 20x
  [6] => 10x
  [7] => 1
  [8] => 1
  [9] => 0x10
  [10] => 0
)

natsort:
array
(
  [10a] => 0
  [4] => 0x10
  [z] => 1
  [2b] => 1
  [x] => 8
  [1] => 10x
  [2] => 20x
  [5] => ach0
  [6] => add1
  [3] => x2
  [0] => x10
)
[file]
<?
setlocale(LC_COLLATE,"cs-CZ");
$sorts = array("sort","ksort","asort","rsort");
$types = array(SORT_REGULAR => "regular",SORT_NUMERIC => "numeric",SORT_STRING => "string",SORT_LOCALE_STRING => "locale");
$array = array("x" => 8,"z" => 1,"2b" => 1,"x10","10a" => 0,"10x","20x","x2","0x10","ach0","add1");

for ($i=0;$i<count($sorts);$i++)
{
  foreach($types as $type => $type_name)
  {
    echo "\n{$sorts[$i]} $type_name:\n";
    $x = $array;
    $sorts[$i]($x,$type);
    print_r($x);
  }  
}

echo "\nnatsort:\n";
$x = $array;
natsort($x);
print_r($x);
?>