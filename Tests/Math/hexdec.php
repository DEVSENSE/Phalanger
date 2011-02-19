[expect php]
[file]

<?php
print(hexdec("See"));
print(hexdec("ee"));
// both print "int(238)"

print(hexdec("that")); // print "int(10)"
print(hexdec("a0")); // print "int(160)"
?> 