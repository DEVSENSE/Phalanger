[expect php]
[file]
<?php
function test()
{
  $double = 6.32;
  $serialized = serialize($double);
  $bad_double = unserialize($serialized);
  $good_double = (double)"6.32";
}

// Run the test
test();