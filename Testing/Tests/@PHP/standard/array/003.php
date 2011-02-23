[expect php]
[file]
<?php
include('Phalanger.inc');
require('data.inc');

function cmp ($a, $b) {
    is_array ($a)
        and $a = array_sum ($a);
    is_array ($b)
        and $b = array_sum ($b);
    return strcmp ($a, $b);
}

echo " -- Testing uasort() -- \n";
uasort ($data, 'cmp');
__var_dump ($data);


echo "\n -- Testing uksort() -- \n";
uksort ($data, 'cmp');
__var_dump ($data);

echo "\n -- Testing usort() -- \n";
usort ($data, 'cmp');
__var_dump ($data);
?>