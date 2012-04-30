[expect php]
[file]
<?

function test()
{
    $a = 1;
    $b = 2;

    $x = function($p, $q, $r = 123) use ($a, &$b)
    {
        echo $a, $b++, $p, $q;
        echo __FUNCTION__;
    };
    $x(1,2);
    $x(1,2);
    
    var_dump($x);
}
test();