echo "===PHP==="
..\..\Tools\PHP\php.exe preg-replace.php
pause
echo "===PHALANGER (COMPILE)==="
phpc preg-replace.php
echo "===PHALANGER==="
bin\preg-replace.exe