[expect php]
[file]
<?
    function foo(&$x)
    {
        $x ++;

        return 2;
    }

    $x = 0;

    echo "+=\n";
    for ($i = 0; $i < 10; $i++)
    {
        echo "$x,";
	    $x += foo($x);
        echo "$x\n";
    }

    echo "-=\n";
    for ($i = 0; $i < 10; $i++)
    {
        echo "$x,";
	    $x -= foo($x);
        echo "$x\n";
    }

    echo "*=\n";
    for ($i = 0; $i < 10; $i++)
    {
        echo "$x,";
	    $x *= foo($x);
        echo "$x\n";
    }

    echo "/=\n";
    for ($i = 0; $i < 10; $i++)
    {
        echo "$x,";
	    $x /= foo($x);
        echo "$x\n";
    }

    echo "&=\n";
    for ($i = 0; $i < 10; $i++)
    {
        echo "$x,";
	    $x &= foo($x);
        echo "$x\n";
    }
?>
