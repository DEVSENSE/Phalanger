[expect php]

[file]
<?php
class A { }
class B { }

$thing = new A;

if ($thing instanceof A) {
    echo 'A';
}
if ($thing instanceof B) {
    echo 'B';
}
?>
