<?php

require_once("Autoloader.php");

// When a class is instantiated via Autoloader from php script,
// it works fine and the args to the ctor are passed correctly.
// However, when a class is instantiated from .Net using ScriptContext.NewObject,
// the ctor params aren't passed to the class and seem to be lost.
//$x = new Klass("Bazzinga");
//$x->foo("bar");

?>