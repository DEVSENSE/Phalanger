[expect php]

[file]
<?php
echo bin2hex(12 ^ 9),"\n"; // Outputs '5'

echo bin2hex("12" ^ "9"),"\n"; // Outputs the Backspace character (ascii 8)
                 // ('1' (ascii 49)) ^ ('9' (ascii 57)) = #8

echo bin2hex("hallo" ^ "hello"),"\n"; // Outputs the ascii values #0 #4 #0 #0 #0
                        // 'a' ^ 'e' = #4
?>
