[expect php]
[file]
<?php
include('Phalanger.inc');
/* From http://bugs.php.net/19865 */
echo md5(__var_dump(explode("\1", "a". chr(1). "b". chr(0). "d" . chr(1) . "f" . chr(1). "1" . chr(1) . "d"), TRUE));
echo "\n";
__var_dump(@explode("", ""));
__var_dump(@explode("", NULL));
__var_dump(@explode(NULL, ""));
__var_dump(@explode("a", ""));
__var_dump(@explode("a", "a"));
__var_dump(@explode("a", NULL));
__var_dump(@explode(NULL, a));
__var_dump(@explode("abc", "acb"));
__var_dump(@explode("somestring", "otherstring"));
__var_dump(@explode("a", "aaaaaa"));
__var_dump(@explode("==", str_repeat("-=".ord(0)."=-", 10)));
__var_dump(@explode("=", str_repeat("-=".ord(0)."=-", 10)));
//////////////////////////////////////
__var_dump(explode(":","a lazy dog:jumps:over:",-1));
__var_dump(explode(":","a lazy dog:jumps:over", -1));
__var_dump(explode(":","a lazy dog:jumps:over", -2));
__var_dump(explode(":","a lazy dog:jumps:over:",-4));
__var_dump(explode(":","a lazy dog:jumps:over:",-40000000000000));
__var_dump(explode(":^:","a lazy dog:^:jumps::over:^:",-1));
__var_dump(explode(":^:","a lazy dog:^:jumps::over:^:",-2));
?>