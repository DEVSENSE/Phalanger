<?php

require_once(dirname(__FILE__)."/../Extensions.php");
require_once(dirname(__FILE__)."/Prototypes.php");

class PHP_Shell_Extensions_InlineHelp implements PHP_Shell_Extension {
    public function register() {
        $cmd = PHP_Shell_Commands::getInstance();

        $cmd->registerCommand('#^\? #', $this, 'cmdHelp', '? <var>', 
            'show the DocComment a Class, Method or Function'.PHP_EOL.
            '    e.g.: ? fopen(), ? PHP_Shell, ? $__shell');
    }

    /**
    * handle the '?' commands
    *
    * With the help of the Reflection Class we extract the DocComments and display them
    * For internal Functions we extract the prototype from the php source.
    *
    * ? Class::method()
    * ? $obj->method()
    * ? Class::property
    * ? $obj::property
    * ? Class
    * ? $obj
    * ? function()
    *
    * The license of the PHP_Shell class
    * ? license
    *
    * @return string the help text
    */
    public function cmdHelp($l) {
        if ("? " == substr($l, 0, strlen("? "))) {
            $str = substr($l, 2);

            $cmd = '';
            
            if (preg_match('#^([A-Za-z0-9_]+)::([a-zA-Z0-9_]+)\(\s*\)\s*#', $str, $a)) {
                /* ? Class::method() */

                $class = $a[1];
                $method = $a[2];

                if (false !== ($proto = PHP_ShellPrototypes::getInstance()->get($class.'::'.$method))) {

                    $cmd = sprintf("/**\n* %s\n\n* @params %s\n* @return %s\n*/\n",
                        $proto['description'],
                        $proto['params'],
                        $proto['return']
                    );
                } else if (class_exists($class, false)) {
                    $c = new ReflectionClass($class);

                    if ($c->hasMethod($method)) {
                        $cmd = $c->getMethod($method)->getDocComment();
                    }
                }
            } else if (preg_match('#^\$([A-Za-z0-9_]+)->([a-zA-Z0-9_]+)\(\s*\)\s*#', $str, $a)) {
                /* ? $obj->method() */
                if (isset($GLOBALS[$a[1]]) && is_object($GLOBALS[$a[1]])) {
                    $class = get_class($GLOBALS[$a[1]]);
                    $method = $a[2];
                    
                    $c = new ReflectionClass($class);
    
                    if ($c->hasMethod($method)) {
                        $cmd = $c->getMethod($method)->getDocComment();
                    }
                }
            } else if (preg_match('#^([A-Za-z0-9_]+)::([a-zA-Z0-9_]+)\s*$#', $str, $a)) { 
                /* ? Class::property */
                $class = $a[1];
                $property = $a[2];
                if (class_exists($class, false)) {
                    $c = new ReflectionClass($class);

                    if ($c->hasProperty($property)) {
                        $cmd = $c->getProperty($property)->getDocComment();
                    }
                }
            } else if (preg_match('#^\$([A-Za-z0-9_]+)->([a-zA-Z0-9_]+)\s*$#', $str, $a)) { 
                /* ? $obj->property */
                if (isset($GLOBALS[$a[1]]) && is_object($GLOBALS[$a[1]])) {
                    $class = get_class($GLOBALS[$a[1]]);
                    $method = $a[2];
                    
                    $c = new ReflectionClass($class);

                    if ($c->hasProperty($property)) {
                        $cmd = $c->getProperty($property)->getDocComment();
                    }

                }
            } else if (preg_match('#^([A-Za-z0-9_]+)$#', $str, $a)) {
                /* ? Class */
                if (class_exists($a[1], false)) {
                    $c = new ReflectionClass($a[1]);
                    $cmd = $c->getDocComment();
                }
            } else if (preg_match('#^\$([A-Za-z0-9_]+)$#', $str, $a)) {
                /* ? $object */
                $obj = $a[1];
                if (isset($GLOBALS[$obj]) && is_object($GLOBALS[$obj])) {
                    $class = get_class($GLOBALS[$obj]);

                    $c = new ReflectionClass($class);
                    $cmd = $c->getDocComment();
                }

            } else if (preg_match('#^([A-Za-z0-9_]+)\(\s*\)$#', $str, $a)) {
                /* ? function() */
                $func = $a[1];

                if (false !== ($proto = PHP_ShellPrototypes::getInstance()->get($func))) {
                    $cmd = sprintf("/**\n* %s\n*\n* @params %s\n* @return %s\n*/\n",
                        $proto['description'],
                        $proto['params'],
                        $proto['return']
                    );
                } else if (function_exists($func)) {
                    $c = new ReflectionFunction($func);
                    $cmd = $c->getDocComment();
                }
            }

            if ($cmd == '') {
                $cmd = var_export(sprintf('no help found for \'%s\'', $str), 1);
            } else {
                $cmd = var_export($cmd, 1);
            }
        } else if ("?" == $l) {
            $cmd = $this->getHelp();
            $cmd = var_export($cmd, 1);
        }

        return $cmd;
    }
}
