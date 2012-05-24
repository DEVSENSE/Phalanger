<?php
/**
* Autoload Extension
*
* Note: shell wrapper has to create the __autoload() function when
*       isAutoloadEnabled() is true
*
* handles the options to enable the internal autoload support
*
* :set al
* :set autoload
*
* autoload can't be disabled
*/
    
class PHP_Shell_Extensions_Autoload implements PHP_Shell_Extension {
    /**
    * does the use want to use the internal autoload ? 
    *
    * @var bool
    */
    protected $autoload = false;

    public function register() {
        $opt = PHP_Shell_Options::getInstance();

        $opt->registerOption("autoload", $this, "optSetAutoload");
        $opt->registerOptionAlias("al", "autoload");
    }

    /**
    * sets the autoload-flag
    *
    * - the $value is ignored and doesn't have to be set
    * - if __autoload() is defined, the set fails
    */
    public function optSetAutoload($key, $value) {
        if ($this->autoload) {
            print('autload is already enabled');
            return;
        }

        if (function_exists('__autoload')) {
            print('can\'t enabled autoload as a external __autoload() function is already defined');
            return;
        }

        $this->autoload = true;
    }

    /**
    * is the autoload-flag set ?
    *
    * @return bool true if __autoload() should be set by the external wrapper
    */
    public function isAutoloadEnabled() {
        return $this->autoload;
    }
}

