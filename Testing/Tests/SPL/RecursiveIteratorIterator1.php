[expect php]
[file]
<?php 
$arr = array(
    'Zero',
    'name'=>'Adil',
    'address' => array(
        'city'=>'Dubai',
        'tel' => array(
            'int' => 971,
            'tel'=>12345487)),
    '' => 'nothing');

$iterator = new RecursiveIteratorIterator(new RecursiveArrayIterator($arr)); 

foreach ($iterator as $k => $v)
{
    echo "$k => $v\n";
}

?>