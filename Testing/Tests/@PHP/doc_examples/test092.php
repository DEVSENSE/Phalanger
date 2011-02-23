[expect php]

[file]
<?php
  for ($i = 0; $i < 5; ++$i) {
      if ($i == 2)
          continue
      print "$i\n";
  }
?>
