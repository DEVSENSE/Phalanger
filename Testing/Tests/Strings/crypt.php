[expect php]
[file]
<?php

$p = "My1sTpassword";
$password = crypt($p); // let salt be generated

# You should pass the entire results of crypt() as the salt for comparing a
# password, to avoid problems when different hashing algorithms are used. (As
# it says above, standard DES-based password hashing uses a 2-character salt,
# but MD5-based hashing uses 12.)

if (crypt($p, $password) == $password) {
   echo "Password verified!";
}

?>  