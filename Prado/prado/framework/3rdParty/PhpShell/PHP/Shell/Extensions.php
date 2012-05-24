<?php

/**
* the interface for all shell extensions 
*
* Extension can hook into the execution of the shell
*
* examples:
* - execution time for parsing and execute
* - colours for the output
* - inline help
*
*  
*/
interface PHP_Shell_Extension {
    public function register();
}

/**
* storage class for Shell Extensions
*
* 
*/
class PHP_Shell_Extensions {
    /**
    * @var PHP_Shell_Extensions
    */
    static protected $instance;

    /**
    * storage for the extension
    *
    * @var array
    */
    protected $exts = array();

    /**
    * the extension object gives access to the register objects
    * through the a simple $exts->name->...
    *
    * @param string registered name of the extension 
    * @return PHP_Shell_Extension object handle
    */
    public function __get($key) {
        if (!isset($this->exts[$key])) {
            throw new Exception("Extension $s is not known.");
        }
        return $this->exts[$key];
    }

    /**
    * register set of extensions
    *
    * @param array set of (name, class-name) pairs
    */
    public function registerExtensions($exts) {
        foreach ($exts as $k => $v) {
            $this->registerExtension($k, $v);
        }
    }

    /**
    * register a single extension
    *
    * @param string name of the registered extension
    * @param PHP_Shell_Extension the extension object
    */
    public function registerExtension($k, PHP_Shell_Extension $obj) {
        $obj->register();

        $this->exts[$k] = $obj;
    }

    /**
    * @return object a singleton of the class 
    */
    static function getInstance() {
        if (is_null(self::$instance)) {
            $class = __CLASS__;
            self::$instance = new $class();
        }
        return self::$instance;
    }
}


