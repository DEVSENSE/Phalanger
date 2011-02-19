[comment] 101000110111001100110100php
[expect php]
[file]

<?php

$hexadecimal = 'A37334';
echo base_convert($hexadecimal, 16, 2) . base_convert('25', 10, 36) . base_convert('1363',7,30);

//var_dump(base_convert(null, 10, 10));
//var_dump(base_convert(0, 0, 2));
//echo base_convert(0, 10, 1);
//echo base_convert(0, 10, 50);

echo "~".base_convert(-555, 10, 10);
echo "~".base_convert(false, 16, 2);
echo "~".@base_convert(array('Q',2,3), 36, 36);
echo "~".base_convert('%', 16, 2);
echo "~".base_convert('Aa', 20, 10);
echo "~".base_convert('Aa', 10, 20);
?> 