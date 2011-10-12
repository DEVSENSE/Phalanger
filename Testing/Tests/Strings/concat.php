[expect exact]
string(2) "ab"
string(2) "ab"
string(2) "ab"
string(2) "ab"
string(2) "ab"
string(2) "aB"
string(2) "Ab"
string(2) "AB"
string(2) "ab"
string(2) "a1"
string(2) "1b"
string(2) "11"
string(2) "ab"
string[binary](2) "aB"
string[binary](2) "Ab"
string[binary](2) "AB"

[file]
<?
  function f()
  {
    $a = "a";
    $b = "b";
    
    var_dump("a" . "b");
    var_dump("a" . $b);
    var_dump($a . "b");
    var_dump($a . $b);
    
    var_dump("a" . "b");
    var_dump("a" . strtoupper("b"));
    var_dump(strtoupper("a") . "b");
    var_dump(strtoupper("a") . strtoupper("b"));
    
    var_dump("a" . "b");
    var_dump("a" . count("a"));
    var_dump(count("a") . "b");
    var_dump(count("a") . count("b"));
    
    var_dump("a" . "b");
    var_dump("a" . pack("c",66));
    var_dump(pack("c",65) . "b");
    var_dump(pack("c",65) . pack("c",66));
  }

  f();
?>  