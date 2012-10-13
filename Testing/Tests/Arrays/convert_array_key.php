[expect php]
[file]
<?  
    // test conversion of string to array key (string or integer):

	$response = array(
        "1608630144506594e510521" => 0,
        "123" => 0,
        "1b" => 0,
        "0123" => 0,
        "0" => 0,
        "000" => 0,
        "" => 0,
        "-" => 0,
        "-0" => 0,
        "-0123" => 0,
        "123456789" => 0,
        "1234567890" => 0,
        "12345678901" => 0,
        "-1234567890" => 0);
    
    foreach ($response as $k => $v)
        var_dump($k);
?>