[expect] 1

[file]
<?php
  
  class A {
      var $one = 1;
    
      function show_one() {
          echo $this->one;
      }
  }
  

  
  $a = new A;
  $s = serialize($a);
  // store $s somewhere where page2.php can find it.
  $fp = fopen("store", "w");
  fwrite($fp, $s);
  fclose($fp);


  $s = implode("", @file("store"));
  $a = unserialize($s);

  // now use the function show_one() of the $a object.  
  $a->show_one();
?>
