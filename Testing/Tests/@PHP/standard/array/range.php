[expect php]
[file]
<?php
include('Phalanger.inc');

__var_dump(range(1, 100));
__var_dump(range(100, 1));

__var_dump(range("1", "100"));
__var_dump(range("100", "1"));

__var_dump(range("a", "z"));
__var_dump(range("z", "a"));
__var_dump(range("q", "q"));

__var_dump(range(5, 5));

__var_dump(range(5.1, 10.1));
__var_dump(range(10.1, 5.1));

__var_dump(range("5.1", "10.1"));
__var_dump(range("10.1", "5.1"));

__var_dump(range(1, 5, 0.1));
__var_dump(range(5, 1, 0.1));

__var_dump(range(1, 5, "0.1"));
__var_dump(range("1", "5", 0.1));
?>