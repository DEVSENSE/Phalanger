[expect php]

[file]
<?php
$action = "show_version";
$show_separators = true;

// == is an operator which test
// equality and returns a bool
if ($action == "show_version") {
    echo "The version is 1.23";
}

// this is not necessary...
if ($show_separators == TRUE) {
    echo "separator\n";
}

// ...because you can simply type
if ($show_separators) {
    echo "separator\n";
}
?>
