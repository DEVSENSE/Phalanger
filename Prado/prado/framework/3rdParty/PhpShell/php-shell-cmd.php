<?php

$f = <<<EOF
PHP-Shell - Version %s%s
(c) 2006, Jan Kneschke <jan@kneschke.de>

>> use '?' to open the inline help

EOF;

printf($f,
    $__shell->getVersion(),
    $__shell->hasReadline() ? ', with readline() support' : '');
unset($f);

print $__shell_exts->colour->getColour("default");
while($__shell->input()) {
    if ($__shell_exts->autoload->isAutoloadEnabled() && !function_exists('__autoload')) {
        /**
        * default autoloader
        *
        * If a class doesn't exist try to load it by guessing the filename
        * class PHP_Shell should be located in PHP/Shell.php.
        *
        * you can set your own autoloader by defining __autoload() before including
        * this file
        *
        * @param string $classname name of the class
        */

        function __autoload($classname) {
            global $__shell_exts;

            if ($__shell_exts->autoload_debug->isAutoloadDebug()) {
                print str_repeat(".", $__shell_exts->autoload_debug->incAutoloadDepth())." -> autoloading $classname".PHP_EOL;
            }
            include_once str_replace('_', '/', $classname).'.php';
            if ($__shell_exts->autoload_debug->isAutoloadDebug()) {
                print str_repeat(".", $__shell_exts->autoload_debug->decAutoloadDepth())." <- autoloading $classname".PHP_EOL;
            }
        }
    }

    try {
        $__shell_exts->exectime->startParseTime();
        if ($__shell->parse() == 0) {
            ## we have a full command, execute it

            $__shell_exts->exectime->startExecTime();

            $__shell_retval = eval($__shell->getCode());
            if (isset($__shell_retval)) {
                print $__shell_exts->colour->getColour("value");

                if (function_exists("__shell_print_var")) {
                    __shell_print_var($__shell,$__shell_retval, $__shell_exts->verboseprint->isVerbose());
                } else {
                    var_export($__shell_retval);
                }
            }
            ## cleanup the variable namespace
            unset($__shell_retval);
            $__shell->resetCode();
        }
    } catch(Exception $__shell_exception) {
        print $__shell_exts->colour->getColour("exception");
        printf('%s (code: %d) got thrown'.PHP_EOL, get_class($__shell_exception), $__shell_exception->getCode());
        print $__shell_exception;

        $__shell->resetCode();

        ## cleanup the variable namespace
        unset($__shell_exception);
    }
    print $__shell_exts->colour->getColour("default");
    $__shell_exts->exectime->stopTime();
    if ($__shell_exts->exectime->isShow()) {
        printf(" (parse: %.4fs, exec: %.4fs)",
            $__shell_exts->exectime->getParseTime(),
            $__shell_exts->exectime->getExecTime()
        );
    }
}

print $__shell_exts->colour->getColour("reset");

